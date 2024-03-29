﻿using System.Text;

namespace JetBlackEngineLib.Data.CutScenes;

public static class CutDecoder
{
    public static CutScene Decode(byte[] data, int startOffset, int length)
    {
        if (length == 0)
        {
            return new CutScene();
        }

        DataReader reader = new(data, startOffset, length);

        CutScene scene = new()
        {
            flags = reader.ReadInt32(),
            keyframeOffset = reader.ReadInt32(),
            numKeyframes = reader.ReadInt32(),
            characterBlockOffset = reader.ReadInt32(),
            numCharacters = reader.ReadInt32()
        };

        reader.SetOffset(0x20);
        scene.subtitles = reader.ReadZString();
        reader.SetOffset(0x48);
        scene.string48 = reader.ReadZString();
        reader.SetOffset(0x70);
        scene.scale = reader.ReadInt32();
        scene.string74 = reader.ReadZString();

        for (var c = 0; c < scene.numCharacters; ++c)
        {
            CutScene.Character character = new();

            var charOffset = scene.characterBlockOffset + (c * 0x2C);
            reader.SetOffset(charOffset);
            character.extra0 = reader.ReadInt32();
            character.extra4 = reader.ReadInt32();
            character.name = reader.ReadZString();
            reader.SetOffset(charOffset + 0x1C);
            character.x = reader.ReadFloat();
            character.y = reader.ReadFloat();
            character.z = reader.ReadFloat();
            character.s28 = reader.ReadInt16();
            scene.Cast.Add(character);
        }

        for (var kf = 0; kf < scene.numKeyframes; ++kf)
        {
            CutScene.Keyframe keyframe = new();
            var kfOffset = scene.keyframeOffset + (kf * 0x14);
            reader.SetOffset(kfOffset);
            keyframe.time = reader.ReadFloat();
            keyframe.actor = reader.ReadInt16();
            keyframe.action = reader.ReadInt16();
            keyframe.i8 = reader.ReadInt32();
            keyframe.ic = reader.ReadInt32();
            keyframe.i10 = reader.ReadInt32();
            if (keyframe.actor == -100 && keyframe.action == 5)
            {
                reader.Rewind(12);
                keyframe.f8 = reader.ReadFloat();
                keyframe.fc = reader.ReadFloat();
                keyframe.f10 = reader.ReadFloat();
            }

            scene.keyframes.Add(keyframe);
        }

        return scene;
    }


    public class CutScene
    {
        public readonly List<Character> Cast = new();

        public int flags, keyframeOffset, numKeyframes, characterBlockOffset, numCharacters;

        public List<Keyframe> keyframes = new();
        public string parseWarnings = "";
        public int scale;

        public string? subtitles, string48, string74;

        private string? GetActorName(int actorNum)
        {
            if (actorNum == -100)
            {
                return "camera";
            }

            if (actorNum == -99)
            {
                return "sound";
            }

            return Cast[actorNum].name;
        }

        private string DisassembleKeyframe(Keyframe keyframe)
        {
            var actorName = GetActorName(keyframe.actor);

            StringBuilder sb = new();
            sb.AppendFormat("time: {0}, {1} ", keyframe.time, actorName);
            var understood = false;
            switch (keyframe.actor)
            {
                case -100:
                    // camera
                    switch (keyframe.action)
                    {
                        case 0:
                            sb.AppendFormat(" pos: {0}, {1}, {2}", keyframe.i8, keyframe.ic, keyframe.i10);
                            understood = true;
                            break;
                        case 1:
                            sb.AppendFormat(" rotate z, {0} deg", keyframe.i8 * 360.0 / 65535.0);
                            understood = true;
                            break;
                        case 2:
                        {
                            var adjustedAngle = (keyframe.i8 + 0x3fd3) & 0xFFFF; // +89 degrees
                            sb.AppendFormat(" rotate x, {0} deg", adjustedAngle * 360.0 / 65535.0);
                            understood = true;
                        }
                            break;
                        case 5:
                            sb.AppendFormat(" delta: {0}, 0x{1:x}, {2}", keyframe.f8, keyframe.ic, keyframe.f10);
                            understood = true;
                            break;
                        case 6:
                            sb.AppendFormat(" farClip = {0}", keyframe.i8 * 12);
                            understood = true;
                            break;
                    }

                    break;
                case -99:
                    switch (keyframe.action)
                    {
                        case 0:
                            sb.Append(" step to next subtitle");
                            understood = true;
                            break;
                    }

                    break;
                default:
                    switch (keyframe.action)
                    {
                        case 0:
                            sb.AppendFormat(" set target pos: {0}, {1}, {2}", keyframe.i8, keyframe.ic,
                                keyframe.i10);
                            understood = true;
                            break;
                        case 2:
                            sb.AppendFormat(" play anim: {0:x4}, {1:x4}, {2:x4}", keyframe.i8, keyframe.ic,
                                keyframe.i10);
                            understood = true;
                            break;
                    }

                    break;
            }

            if (!understood)
            {
                sb.AppendFormat("action: {0}, data: {1:x4} {2:x4} {3:x4}",
                    keyframe.action, keyframe.i8, keyframe.ic, keyframe.i10);
            }

            return sb.ToString();
        }

        public string Disassemble()
        {
            StringBuilder sb = new();
            if (!string.IsNullOrEmpty(parseWarnings))
            {
                sb.Append("Warnings:\n").Append(parseWarnings).Append("\n");
            }

            sb.AppendFormat("Flags:                0x{0:x4}\n", flags);
            sb.AppendFormat("keyframeOffset:       0x{0:x4}\n", keyframeOffset);
            sb.AppendFormat("numKeyframes:         0x{0}\n", numKeyframes);
            sb.AppendFormat("characterBlockOffset: 0x{0:x4}\n", characterBlockOffset);
            sb.AppendFormat("numCharacters:        0x{0}\n", numCharacters);
            sb.AppendFormat("scale:     {0}\n", scale * 0.1);
            sb.AppendFormat("subtitles: {0}\n", subtitles);
            sb.AppendFormat("string48:  {0}\n", string48);
            sb.AppendFormat("string74:  {0}\n", string74);

            sb.Append("\nCast\n~~~~\n");
            foreach (var c in Cast)
            {
                sb.Append(c.Disassemble()).Append('\n');
            }

            sb.Append("\nKeyframes\n~~~~~~~~~\n");
            foreach (var kf in keyframes)
            {
                sb.Append(DisassembleKeyframe(kf)).Append('\n');
            }

            return sb.ToString();
        }

        public class Character
        {
            public int extra0, extra4;
            public string? name;
            public short s28;
            public float x, y, z;

            public string Disassemble()
            {
                var rot = (s28 >> 12) * 22.5;

                StringBuilder sb = new();
                sb.AppendFormat(
                    "name: {0}, pos: {1}, {2}, {3}, rot: {4}, extra0: 0x{5:x4}, extra4: 0x{6:x4}, s28: 0x{7:x4}",
                    name, x, y, z, rot, extra0, extra4, s28);
                return sb.ToString();
            }
        }

        public class Keyframe
        {
            public int actor, action;
            public float f8, fc, f10;
            public int i8, ic, i10;
            public float time;
        }
    }
}