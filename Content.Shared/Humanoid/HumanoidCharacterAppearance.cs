// SPDX-FileCopyrightText: 2019 Pieter-Jan Briers <pieterjan.briers@gmail.com>
// SPDX-FileCopyrightText: 2020 DamianX <DamianX@users.noreply.github.com>
// SPDX-FileCopyrightText: 2020 Víctor Aguilera Puerto <6766154+Zumorica@users.noreply.github.com>
// SPDX-FileCopyrightText: 2021 Acruid <shatter66@gmail.com>
// SPDX-FileCopyrightText: 2021 Metal Gear Sloth <metalgearsloth@gmail.com>
// SPDX-FileCopyrightText: 2021 Moses <StrawberryMoses@gmail.com>
// SPDX-FileCopyrightText: 2021 Pieter-Jan Briers <pieterjan.briers+git@gmail.com>
// SPDX-FileCopyrightText: 2021 Visne <39844191+Visne@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 metalgearsloth <comedian_vs_clown@hotmail.com>
// SPDX-FileCopyrightText: 2022 mirrorcult <lunarautomaton6@gmail.com>
// SPDX-FileCopyrightText: 2022 wrexbe <81056464+wrexbe@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Flipp Syder <76629141+vulppine@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Morb <14136326+Morb0@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 csqrb <56765288+CaptainSqrBeard@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Errant <35878406+Errant-4@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2025 SX-7 <92227810+SX-7@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 coderabbitai[bot] <136622811+coderabbitai[bot]@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared.Humanoid;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class HumanoidCharacterAppearance : ICharacterAppearance, IEquatable<HumanoidCharacterAppearance>
{
    [DataField("hair")]
    public string HairStyleId { get; set; } = HairStyles.DefaultHairStyle;

    [DataField]
    public Color HairColor { get; set; } = Color.Black;

    // Dumont - hair gradient - start
    [DataField]
    public Color? HairGradientColor { get; set; }

    [DataField]
    public float HairGradientCoverage { get; set; }
    // Dumont - end

    [DataField("facialHair")]
    public string FacialHairStyleId { get; set; } = HairStyles.DefaultFacialHairStyle;

    [DataField]
    public Color FacialHairColor { get; set; } = Color.Black;

    // Dumont - hair gradient - start
    [DataField]
    public Color? FacialHairGradientColor { get; set; }

    [DataField]
    public float FacialHairGradientCoverage { get; set; }
    // Dumont - end

    [DataField]
    public Color EyeColor { get; set; } = Color.Black;

    [DataField]
    public Color SkinColor { get; set; } = Humanoid.SkinColor.ValidHumanSkinTone;

    [DataField]
    public List<Marking> Markings { get; set; } = new();

    public HumanoidCharacterAppearance(string hairStyleId,
        Color hairColor,
        string facialHairStyleId,
        Color facialHairColor,
        Color eyeColor,
        Color skinColor,
        List<Marking> markings,
        Color? hairGradientColor = null, // Dumont - hair gradient - start
        float hairGradientCoverage = 0f,
        Color? facialHairGradientColor = null,
        float facialHairGradientCoverage = 0f) // Dumont - end
    {
        HairStyleId = hairStyleId;
        HairColor = ClampColor(hairColor);
        FacialHairStyleId = facialHairStyleId;
        FacialHairColor = ClampColor(facialHairColor);
        EyeColor = ClampColor(eyeColor);
        SkinColor = ClampColor(skinColor);
        Markings = markings;
        // Dumont - hair gradient - start
        HairGradientColor = hairGradientColor;
        HairGradientCoverage = Math.Clamp(hairGradientCoverage, -1f, 1f);
        FacialHairGradientColor = facialHairGradientColor;
        FacialHairGradientCoverage = Math.Clamp(facialHairGradientCoverage, -1f, 1f);
        // Dumont - end
    }

    public HumanoidCharacterAppearance(HumanoidCharacterAppearance other) :
        this(other.HairStyleId, other.HairColor, other.FacialHairStyleId, other.FacialHairColor, other.EyeColor, other.SkinColor, new(other.Markings),
            other.HairGradientColor, other.HairGradientCoverage, other.FacialHairGradientColor, other.FacialHairGradientCoverage)
    {

    }

    public HumanoidCharacterAppearance WithHairStyleName(string newName)
    {
        return new(newName, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings,
            HairGradientColor, HairGradientCoverage, FacialHairGradientColor, FacialHairGradientCoverage);
    }

    public HumanoidCharacterAppearance WithHairColor(Color newColor)
    {
        return new(HairStyleId, newColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings,
            HairGradientColor, HairGradientCoverage, FacialHairGradientColor, FacialHairGradientCoverage);
    }

    // Dumont - hair gradient - start
    public HumanoidCharacterAppearance WithHairGradientColor(Color? newColor)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings,
            newColor, HairGradientCoverage, FacialHairGradientColor, FacialHairGradientCoverage);
    }

    public HumanoidCharacterAppearance WithHairGradientCoverage(float newCoverage)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings,
            HairGradientColor, newCoverage, FacialHairGradientColor, FacialHairGradientCoverage);
    }
    // Dumont - end

    public HumanoidCharacterAppearance WithFacialHairStyleName(string newName)
    {
        return new(HairStyleId, HairColor, newName, FacialHairColor, EyeColor, SkinColor, Markings,
            HairGradientColor, HairGradientCoverage, FacialHairGradientColor, FacialHairGradientCoverage); // Dumont - hair gradient
    }

    public HumanoidCharacterAppearance WithFacialHairColor(Color newColor)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, newColor, EyeColor, SkinColor, Markings,
            HairGradientColor, HairGradientCoverage, FacialHairGradientColor, FacialHairGradientCoverage); // Dumont - hair gradient
    }

    // Dumont - hair gradient - start
    public HumanoidCharacterAppearance WithFacialHairGradientColor(Color? newColor)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings,
            HairGradientColor, HairGradientCoverage, newColor, FacialHairGradientCoverage);
    }

    public HumanoidCharacterAppearance WithFacialHairGradientCoverage(float newCoverage)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings,
            HairGradientColor, HairGradientCoverage, FacialHairGradientColor, newCoverage);
    }
    // Dumont - end

    public HumanoidCharacterAppearance WithEyeColor(Color newColor)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, newColor, SkinColor, Markings,
            HairGradientColor, HairGradientCoverage, FacialHairGradientColor, FacialHairGradientCoverage);
    }

    public HumanoidCharacterAppearance WithSkinColor(Color newColor)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, newColor, Markings,
            HairGradientColor, HairGradientCoverage, FacialHairGradientColor, FacialHairGradientCoverage);
    }

    public HumanoidCharacterAppearance WithMarkings(List<Marking> newMarkings)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, newMarkings,
            HairGradientColor, HairGradientCoverage, FacialHairGradientColor, FacialHairGradientCoverage);
    }

    public static HumanoidCharacterAppearance DefaultWithSpecies(string species)
    {
        var speciesPrototype = IoCManager.Resolve<IPrototypeManager>().Index<SpeciesPrototype>(species);
        var skinColor = speciesPrototype.SkinColoration switch
        {
            HumanoidSkinColor.HumanToned => Humanoid.SkinColor.HumanSkinTone(speciesPrototype.DefaultHumanSkinTone),
            HumanoidSkinColor.Hues => speciesPrototype.DefaultSkinTone,
            HumanoidSkinColor.TintedHues => Humanoid.SkinColor.TintedHues(speciesPrototype.DefaultSkinTone),
            HumanoidSkinColor.VoxFeathers => Humanoid.SkinColor.ClosestVoxColor(speciesPrototype.DefaultSkinTone),
            HumanoidSkinColor.AnimalFur => Humanoid.SkinColor.ClosestAnimalFurColor(speciesPrototype.DefaultSkinTone), // Goobstation - Tajaran
            _ => Humanoid.SkinColor.ValidHumanSkinTone,
        };

        return new(
            HairStyles.DefaultHairStyle,
            Color.Black,
            HairStyles.DefaultFacialHairStyle,
            Color.Black,
            Color.Black,
            skinColor,
            new ()
        );
    }

    private static IReadOnlyList<Color> RealisticEyeColors = new List<Color>
    {
        Color.Brown,
        Color.Gray,
        Color.Azure,
        Color.SteelBlue,
        Color.Black
    };

    public static HumanoidCharacterAppearance Random(string species, Sex sex)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        var markingManager = IoCManager.Resolve<MarkingManager>();
        var hairStyles = markingManager.MarkingsByCategoryAndSpecies(MarkingCategories.Hair, species).Keys.ToList();
        var facialHairStyles = markingManager.MarkingsByCategoryAndSpecies(MarkingCategories.FacialHair, species).Keys.ToList();

        var newHairStyle = hairStyles.Count > 0
            ? random.Pick(hairStyles)
            : HairStyles.DefaultHairStyle.Id;

        var newFacialHairStyle = facialHairStyles.Count == 0 || sex == Sex.Female
            ? HairStyles.DefaultFacialHairStyle.Id
            : random.Pick(facialHairStyles);

        var newHairColor = random.Pick(HairStyles.RealisticHairColors);
        newHairColor = newHairColor
            .WithRed(RandomizeColor(newHairColor.R))
            .WithGreen(RandomizeColor(newHairColor.G))
            .WithBlue(RandomizeColor(newHairColor.B));

        // TODO: Add random markings

        var newEyeColor = random.Pick(RealisticEyeColors);

        var skinType = IoCManager.Resolve<IPrototypeManager>().Index<SpeciesPrototype>(species).SkinColoration;

        var newSkinColor = new Color(random.NextFloat(1), random.NextFloat(1), random.NextFloat(1), 1);
        switch (skinType)
        {
            case HumanoidSkinColor.HumanToned:
                newSkinColor = Humanoid.SkinColor.HumanSkinTone(random.Next(0, 101));
                break;
            case HumanoidSkinColor.Hues:
                break;
            case HumanoidSkinColor.TintedHues:
                newSkinColor = Humanoid.SkinColor.ValidTintedHuesSkinTone(newSkinColor);
                break;
            case HumanoidSkinColor.VoxFeathers:
                newSkinColor = Humanoid.SkinColor.ProportionalVoxColor(newSkinColor);
                break;
            case HumanoidSkinColor.AnimalFur:
                newSkinColor = Humanoid.SkinColor.ProportionalAnimalFurColor(newSkinColor); // Goobstation - Tajaran
                break;
        }

        return new HumanoidCharacterAppearance(newHairStyle, newHairColor, newFacialHairStyle, newHairColor, newEyeColor, newSkinColor, new ());

        float RandomizeColor(float channel)
        {
            return MathHelper.Clamp01(channel + random.Next(-25, 25) / 100f);
        }
    }

    public static Color ClampColor(Color color)
    {
        return new(color.RByte, color.GByte, color.BByte);
    }

    public static HumanoidCharacterAppearance EnsureValid(HumanoidCharacterAppearance appearance, string species, Sex sex)
    {
        var hairStyleId = appearance.HairStyleId;
        var facialHairStyleId = appearance.FacialHairStyleId;

        var hairColor = ClampColor(appearance.HairColor);
        var facialHairColor = ClampColor(appearance.FacialHairColor);
        var eyeColor = ClampColor(appearance.EyeColor);

        var proto = IoCManager.Resolve<IPrototypeManager>();
        var markingManager = IoCManager.Resolve<MarkingManager>();

        if (!markingManager.MarkingsByCategory(MarkingCategories.Hair).ContainsKey(hairStyleId))
        {
            hairStyleId = HairStyles.DefaultHairStyle;
        }

        if (!markingManager.MarkingsByCategory(MarkingCategories.FacialHair).ContainsKey(facialHairStyleId))
        {
            facialHairStyleId = HairStyles.DefaultFacialHairStyle;
        }

        var markingSet = new MarkingSet();
        var skinColor = appearance.SkinColor;
        if (proto.TryIndex(species, out SpeciesPrototype? speciesProto))
        {
            markingSet = new MarkingSet(appearance.Markings, speciesProto.MarkingPoints, markingManager, proto);
            markingSet.EnsureValid(markingManager);

            if (!Humanoid.SkinColor.VerifySkinColor(speciesProto.SkinColoration, skinColor))
            {
                skinColor = Humanoid.SkinColor.ValidSkinTone(speciesProto.SkinColoration, skinColor);
            }

            markingSet.EnsureSpecies(species, skinColor, markingManager);
            markingSet.EnsureSexes(sex, markingManager);
        }

        return new HumanoidCharacterAppearance(
            hairStyleId,
            hairColor,
            facialHairStyleId,
            facialHairColor,
            eyeColor,
            skinColor,
            markingSet.GetForwardEnumerator().ToList(),
            appearance.HairGradientColor, appearance.HairGradientCoverage, // Dumont - hair gradient
            appearance.FacialHairGradientColor, appearance.FacialHairGradientCoverage);
    }

    public bool MemberwiseEquals(ICharacterAppearance maybeOther)
    {
        if (maybeOther is not HumanoidCharacterAppearance other) return false;
        if (HairStyleId != other.HairStyleId) return false;
        if (!HairColor.Equals(other.HairColor)) return false;
        if (FacialHairStyleId != other.FacialHairStyleId) return false;
        if (!FacialHairColor.Equals(other.FacialHairColor)) return false;
        if (!EyeColor.Equals(other.EyeColor)) return false;
        if (!SkinColor.Equals(other.SkinColor)) return false;
        if (!Markings.SequenceEqual(other.Markings)) return false;
        // Dumont - hair gradient - start
        // ts is ugly
        if (!Equals(HairGradientColor, other.HairGradientColor)) return false;
        if (!HairGradientCoverage.Equals(other.HairGradientCoverage)) return false;
        if (!Equals(FacialHairGradientColor, other.FacialHairGradientColor)) return false;
        if (!FacialHairGradientCoverage.Equals(other.FacialHairGradientCoverage)) return false;
        // Dumont - end
        return true;
    }

    public bool Equals(HumanoidCharacterAppearance? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return HairStyleId == other.HairStyleId &&
               HairColor.Equals(other.HairColor) &&
               FacialHairStyleId == other.FacialHairStyleId &&
               FacialHairColor.Equals(other.FacialHairColor) &&
               EyeColor.Equals(other.EyeColor) &&
               SkinColor.Equals(other.SkinColor) &&
               Markings.SequenceEqual(other.Markings) &&
               Equals(HairGradientColor, other.HairGradientColor) && // Dumont -start hair gradient
               HairGradientCoverage.Equals(other.HairGradientCoverage) &&
               Equals(FacialHairGradientColor, other.FacialHairGradientColor) &&
               FacialHairGradientCoverage.Equals(other.FacialHairGradientCoverage); // Dumont - end
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is HumanoidCharacterAppearance other && Equals(other);
    }

    public override int GetHashCode()
    {
        // Dumont
        return HashCode.Combine(
            HashCode.Combine(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings),
            HairGradientColor, HairGradientCoverage, FacialHairGradientColor, FacialHairGradientCoverage);
    }

    public HumanoidCharacterAppearance Clone()
    {
        return new(this);
    }
}
