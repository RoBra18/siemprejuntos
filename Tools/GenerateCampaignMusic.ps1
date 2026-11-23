param([string]$OutputDirectory = (Join-Path $PSScriptRoot '../Assets/GameDuo/Resources/CampaignMusic'))

$ErrorActionPreference = 'Stop'
$source = @"
using System;
using System.IO;

public static class CampaignScoreGenerator {
    const int Rate = 22050;
    const double Tau = 2 * Math.PI;

    static double Frequency(int midi) => 440 * Math.Pow(2, (midi - 69) / 12.0);

    static void Note(float[] mix, double at, double duration, int pitch, double gain, int voice) {
        double frequency = Frequency(pitch);
        int start = (int)Math.Round(at * Rate);
        int length = (int)(duration * Rate);
        double attack = voice == 2 ? .16 : .013;
        for (int i = 0; i < length; i++) {
            double t = i / (double)Rate;
            double p = Tau * frequency * t;
            double envelope = Math.Min(1, t / attack) * Math.Min(1, (duration - t) / .08);
            double tone;
            switch (voice) {
                case 0: tone = (Math.Sin(p) + .25 * Math.Sin(2 * p) + .09 * Math.Sin(3 * p)) * Math.Exp(-3.5 * t); break;
                case 1: tone = Math.Sin(p) + .22 * Math.Sin(3 * p) + .08 * Math.Sin(5 * p); break;
                case 2: tone = .55 * Math.Sin(p) + .25 * Math.Sin(p * 1.002) + .12 * Math.Sin(2 * p); break;
                case 4: tone = (Math.Sin(p) + .18 * Math.Sin(p * 3) + .14 * Math.Sin(p * 4.03)) * Math.Exp(-12 * t); break;
                case 6: tone = (Math.Sin(p) + .43 * Math.Sin(2 * p) + .31 * Math.Sin(3 * p) + .19 * Math.Sin(5 * p)) * Math.Exp(-4 * t); break;
                case 7: tone = (.68 * Math.Sin(p) + .23 * Math.Sin(2.01 * p) + .17 * Math.Sin(5.42 * p)) * Math.Exp(-9 * t); break;
                case 5: tone = (Math.Sin(p) + .26 * Math.Sin(3 * p) + .05 * Math.Sin(p * 1.5)) * Math.Exp(-2 * t); break;
                default: tone = Math.Sin(p) + .16 * Math.Sin(p * 2); break;
            }
            mix[(start + i) % mix.Length] += (float)(tone * gain * envelope);
        }
    }

    static void Drum(float[] mix, double at, bool kick, double gain, Random random) {
        int start = (int)Math.Round(at * Rate);
        for (int i = 0; i < Rate / 4; i++) {
            double t = i / (double)Rate;
            double attack = Math.Min(1, t / .002);
            double tone = kick
                ? Math.Sin(Tau * (43 * t + 1.5 * (1 - Math.Exp(-36 * t)))) * Math.Exp(-18 * t)
                : (2 * random.NextDouble() - 1) * Math.Exp(-48 * t);
            mix[(start + i) % mix.Length] += (float)(tone * attack * gain);
        }
    }

    public static void Render(string path, int bpm, int root, int style, int[] progression, int[][] motif) {
        double beat = 60.0 / bpm;
        // Use a triplet pulse throughout. Pingüinos keeps its original two-beat bars.
        int steps = style == 3 ? 6 : 8;
        double subdivision = style == 3 ? beat / 3 : beat / 2;
        double barSeconds = style == 3 ? steps * subdivision : 4 * beat;
        const int bars = 32;
        var mix = new float[(int)Math.Round(bars * barSeconds * Rate)];
        var random = new Random(1729 + style);
        int[][] bassSteps = { new[]{0,4,7}, new[]{0,1,3,6,7}, new[]{0,3,6}, new[]{0,3,5}, new[]{0,1,2,4,5,6,7} };
        int[][] kickSteps = { new[]{0,4}, new[]{0,3,7}, new[]{0,4,6}, new[]{0,3}, new[]{0,2,5,7} };
        int[][] hatSteps = { new[]{2,5,6}, new[]{1,4,5,6}, new[]{1,3,5,7}, new[]{1,2,4,5}, new[]{1,2,3,4,5,6,7} };
        int[] leadVoice = { 0, 1, 7, 4, 6 };
        for (int bar = 0; bar < bars; bar++) {
            int chord = progression[bar % progression.Length];
            int third = style == 0 || style == 3 ? 4 : 3;
            int harmony = root + chord;
            double start = bar * barSeconds;
            bool middleSection = bar >= 8 && bar < 16 || bar >= 24;
            bool endOfPhrase = bar % 8 == 7;
            if (style == 2) {
                foreach (int interval in new[] { 0, 7, 14 })
                    Note(mix, start, barSeconds * 1.08, harmony + interval, .044, 2);
            }
            if (style == 4) Note(mix, start, barSeconds * .92, root - 12, .045, 2);

            for (int step = 0; step < steps; step++) {
                double at = start + (style == 3 ? step * subdivision : (step / 2.0 + (step % 2) / 6.0) * beat);
                if (style != 3 && step % 2 == 0) {
                    int pulse = step / 2;
                    Drum(mix, at + beat / 3, false, style == 0 ? .022 : .038, random);
                    if (pulse == 0 || pulse == 2)
                        Drum(mix, at + 2 * beat / 3, false, style == 0 ? .04 : .056, random);
                }
                if (Array.IndexOf(bassSteps[style], step) >= 0) {
                    int bass = harmony - 12 + (step == steps - 1 ? 7 : 0);
                    Note(mix, at, beat * (style == 0 ? 1.4 : .38), bass, .10, 3);
                }
                if (style == 0 && (step == 2 || step == 6 || middleSection && step == 5))
                    foreach (int interval in new[] { 0, third, 7 })
                        Note(mix, at, beat * .7, harmony + 12 + interval, .046, 0);
                if (style == 1 && (step == 2 || step == 6 || middleSection && step == 5))
                    Note(mix, at, subdivision * 1.2, harmony + 12 + (step == 4 ? 7 : third), .042, 1);

                if (style == 1 && step == 4)
                    Drum(mix, at, false, .14, random);
                if (style == 4 && (step == 4 || endOfPhrase && step == 6))
                    Drum(mix, at, false, .17, random);

                int phrase = style == 3 ? bar / 2 : bar + (middleSection ? 1 : 0);
                int pitch = motif[phrase % motif.Length][step];
                if (pitch >= 0) {
                    int leadRoot = style == 4 ? root + 12 : harmony + (style == 2 || style == 1 ? 12 : 24);
                    int note = leadRoot + pitch;
                    if (style == 3 && bar >= 16 && step % 3 == 0) note += 12;
                    if (style == 2 && middleSection && step % 2 == 1) note += 12;
                    double length = subdivision * (style == 2 ? 1.35 : style == 3 ? .72 : 1.2);
                    Note(mix, at, length, note, .088, leadVoice[style]);
                    if (style == 2 || style == 1) Note(mix, at + subdivision * 1.5, length, note, .016, leadVoice[style]);
                    if (style == 4 && middleSection && step % 2 == 0)
                        Note(mix, at, length, note - 12, .049, 6);
                }

                if (Array.IndexOf(kickSteps[style], step) >= 0)
                    Drum(mix, at, true, style == 4 ? .19 : style == 0 ? .06 : .12, random);
                if (Array.IndexOf(hatSteps[style], step) >= 0)
                    Drum(mix, at, false, style == 4 ? .08 : .041, random);
            }
        }
        float peak = 0;
        foreach (float sample in mix) peak = Math.Max(peak, Math.Abs(sample));
        using var writer = new BinaryWriter(File.Create(path));
        int bytes = mix.Length * 2;
        writer.Write(new[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' });
        writer.Write(bytes + 36);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(Rate);
        writer.Write(Rate * 2);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        writer.Write(bytes);
        foreach (float sample in mix) writer.Write((short)Math.Round(sample / peak * .62 * 32767));
    }

    public static void Generate(string output) {
        Directory.CreateDirectory(output);
        Render(Path.Combine(output, "Chicle.wav"), 144, 50, 0, new[] { 0, 5, -3, 7, 0, 2, 5, 7 },
            new[] { new[] { 4, -1, 9, 7, -1, 2, 4, -1 }, new[] { -1, 7, 2, -1, 4, 2, -1, 0 },
                    new[] { 0, -1, 2, 4, -1, 9, 7, -1 }, new[] { 5, 4, -1, 2, -1, 0, -1, -1 } });
        Render(Path.Combine(output, "Neon.wav"), 160, 44, 1, new[] { 0, 0, 5, 3, 0, -2, 5, -5 },
            new[] { new[] { -1, 12, -1, 7, -1, 2, -1, -1 }, new[] { 3, -1, -1, 6, -1, 7, -1, 2 },
                    new[] { -1, 0, 7, -1, 10, -1, -1, 12 }, new[] { -1, -1, 7, -1, 3, 2, -1, -1 } });
        Render(Path.Combine(output, "ReyHielo.wav"), 166, 49, 2, new[] { 0, 3, 5, -2, 0, -4, 3, -2 },
            new[] { new[] { 0, -1, 7, 11, -1, 2, 9, -1 }, new[] { -1, 5, 2, -1, 9, -1, 12, 7 },
                    new[] { 7, 2, -1, 5, -1, 11, 9, -1 }, new[] { -1, 2, 6, -1, 11, -1, 1, -1 } });
        Render(Path.Combine(output, "Pinguinos.wav"), 166, 53, 3, new[] { 0, 5, -3, 2 },
            new[] { new[] { 0, 7, 4, 12, 7, 4, -1, -1 }, new[] { 9, 7, 4, 2, 4, 7, -1, -1 } });
        Render(Path.Combine(output, "JefeChicle.wav"), 172, 41, 4, new[] { 0, 0, 1, -2, 0, 3, 1, -5 },
            new[] { new[] { 0, 7, -1, 6, -1, 1, 0, -1 }, new[] { -1, 3, 7, -1, 8, -1, 3, 1 },
                    new[] { 12, -1, 11, 7, -1, 6, -1, 1 }, new[] { 5, -1, -1, 4, 1, -1, 0, -1 } });
    }
}
"@

if (-not ('CampaignScoreGenerator' -as [type])) { Add-Type -TypeDefinition $source }
[CampaignScoreGenerator]::Generate([IO.Path]::GetFullPath($OutputDirectory))