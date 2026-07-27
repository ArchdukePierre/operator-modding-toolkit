using System;
using System.IO;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace RuntimeAssets
{
    // RIFF WAV -> AudioClip without a runtime decoder.
    //
    // UnityWebRequestMultimedia is awkward inside a melon and there is no MP3 decoder available, but
    // AudioClip.Create plus SetData works fine, so parsing the header by hand is the shortest path to
    // custom audio. Convert sources to 16 bit PCM first, for example:
    //   ffmpeg -i in.mp3 -ar 22050 -ac 1 -sample_fmt s16 out.wav
    //
    // The chunk walk is written defensively. A malformed or truncated file must fail rather than spin,
    // so every step checks that the cursor actually advances and stays inside the buffer.
    public static class WavAudio
    {
        public static AudioClip Load(string path, string name = null)
        {
            try
            {
                if (!File.Exists(path)) return null;
                var b = File.ReadAllBytes(path);
                if (b.Length < 44) return null;
                if (b[0] != 'R' || b[1] != 'I' || b[2] != 'F' || b[3] != 'F') return null;

                int channels = 1, sampleRate = 22050, bits = 16, dataOff = -1, dataLen = 0;

                int i = 12;
                while (i + 8 <= b.Length)
                {
                    string id = System.Text.Encoding.ASCII.GetString(b, i, 4);
                    int sz = BitConverter.ToInt32(b, i + 4);
                    if (sz < 0) break;

                    if (id == "fmt " && i + 24 <= b.Length)
                    {
                        channels   = BitConverter.ToInt16(b, i + 10);
                        sampleRate = BitConverter.ToInt32(b, i + 12);
                        bits       = BitConverter.ToInt16(b, i + 22);
                    }
                    else if (id == "data")
                    {
                        dataOff = i + 8;
                        dataLen = sz;
                        break;
                    }

                    long next = (long)i + 8 + sz + (sz & 1);      // chunks are word aligned
                    if (next <= i || next > b.Length) break;      // no progress or overflow, bail
                    i = (int)next;
                }

                if (dataOff < 0 || bits != 16) return null;
                dataLen = Math.Min(dataLen, b.Length - dataOff);
                if (dataLen < 2) return null;

                int samples = dataLen / 2;
                var arr = new Il2CppStructArray<float>(samples);
                for (int s = 0; s < samples; s++)
                    arr[s] = BitConverter.ToInt16(b, dataOff + s * 2) / 32768f;

                int frames = samples / Math.Max(1, channels);
                var clip = AudioClip.Create(name ?? Path.GetFileNameWithoutExtension(path),
                                            frames, channels, sampleRate, false);
                return clip != null && clip.SetData(arr, 0) ? clip : null;
            }
            catch { return null; }
        }

        // Convenience: one shot at a world position, cleaned up automatically.
        public static AudioSource PlayAt(AudioClip clip, Vector3 pos, float volume = 1f, float lifetime = 0f)
        {
            if (clip == null) return null;
            var go = new GameObject("RuntimeAudio_" + clip.name);
            go.transform.position = pos;
            var src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.volume = volume;
            src.spatialBlend = 1f;
            src.Play();
            UnityEngine.Object.Destroy(go, lifetime > 0f ? lifetime : clip.length + 0.5f);
            return src;
        }
    }
}
