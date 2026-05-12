using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace efscriptgen.Tests
{
	public class VariantsParserTests
	{
	private static readonly Assembly _assembly = typeof(VariantsParserTests).Assembly;

	[Fact]
	public void TestBasicVariants()
	{
		var xml = _assembly.ReadResourceAsString("efscriptgen.Tests.Resources.BasicVariants.xml");
		var variants = VariantsParser.FromXml(xml);

		Assert.NotNull(variants);
		Assert.Equal(4, variants.Count);

		// Variant 1: No defines
		var v1 = variants.FirstOrDefault(v => v.Count == 0);
		Assert.NotNull(v1);

		// Variant 2: TEXTURE only
		var v2 = variants.FirstOrDefault(v => v.Count == 1 && v.ContainsKey("TEXTURE"));
		Assert.NotNull(v2);
		Assert.Equal("1", v2["TEXTURE"]);

		// Variant 3: LIGHTNING only
		var v3 = variants.FirstOrDefault(v => v.Count == 1 && v.ContainsKey("LIGHTNING"));
		Assert.NotNull(v3);
		Assert.Equal("1", v3["LIGHTNING"]);

		// Variant 4: Both TEXTURE and LIGHTNING
		var v4 = variants.FirstOrDefault(v => v.Count == 2 && v.ContainsKey("TEXTURE") && v.ContainsKey("LIGHTNING"));
		Assert.NotNull(v4);
		Assert.Equal("1", v4["TEXTURE"]);
		Assert.Equal("1", v4["LIGHTNING"]);
	}

	[Fact]
	public void TestMacrosWithValues()
	{
		var xml = _assembly.ReadResourceAsString("efscriptgen.Tests.Resources.MacrosWithValues.xml");
		var variants = VariantsParser.FromXml(xml);

		Assert.NotNull(variants);
		Assert.Equal(6, variants.Count);

		// QUALITY=0 without TEXTURE
		var vq0 = variants.FirstOrDefault(v => v.ContainsKey("QUALITY") && v["QUALITY"] == "0" && v.Count == 1);
		Assert.NotNull(vq0);

		// QUALITY=0 with TEXTURE
		var vq0t = variants.FirstOrDefault(v => v.ContainsKey("QUALITY") && v["QUALITY"] == "0" && v.ContainsKey("TEXTURE") && v.Count == 2);
		Assert.NotNull(vq0t);
		Assert.Equal("1", vq0t["TEXTURE"]);

		// QUALITY=1 without TEXTURE
		var vq1 = variants.FirstOrDefault(v => v.ContainsKey("QUALITY") && v["QUALITY"] == "1" && v.Count == 1);
		Assert.NotNull(vq1);

		// QUALITY=1 with TEXTURE
		var vq1t = variants.FirstOrDefault(v => v.ContainsKey("QUALITY") && v["QUALITY"] == "1" && v.ContainsKey("TEXTURE") && v.Count == 2);
		Assert.NotNull(vq1t);

		// QUALITY=2 without TEXTURE
		var vq2 = variants.FirstOrDefault(v => v.ContainsKey("QUALITY") && v["QUALITY"] == "2" && v.Count == 1);
		Assert.NotNull(vq2);

		// QUALITY=2 with TEXTURE
		var vq2t = variants.FirstOrDefault(v => v.ContainsKey("QUALITY") && v["QUALITY"] == "2" && v.ContainsKey("TEXTURE") && v.Count == 2);
		Assert.NotNull(vq2t);
	}

	[Fact]
	public void TestGroupedValues()
	{
		var xml = _assembly.ReadResourceAsString("efscriptgen.Tests.Resources.GroupedValues.xml");
		var variants = VariantsParser.FromXml(xml);

		Assert.NotNull(variants);
		Assert.Equal(6, variants.Count);

		// [LOW,LIGHTNING] without TEXTURE
		var vlow = variants.FirstOrDefault(v => v.ContainsKey("LOW") && v.ContainsKey("LIGHTNING") && v.Count == 2 && !v.ContainsKey("TEXTURE"));
		Assert.NotNull(vlow);
		Assert.Equal("1", vlow["LOW"]);
		Assert.Equal("1", vlow["LIGHTNING"]);

		// [LOW,LIGHTNING] with TEXTURE
		var vlowt = variants.FirstOrDefault(v => v.ContainsKey("LOW") && v.ContainsKey("LIGHTNING") && v.ContainsKey("TEXTURE") && v.Count == 3);
		Assert.NotNull(vlowt);

		// [MEDIUM,LIGHTNING,SIMPLESHADOW] without TEXTURE
		var vmed = variants.FirstOrDefault(v =>
			v.ContainsKey("MEDIUM") && v.ContainsKey("LIGHTNING") && v.ContainsKey("SIMPLESHADOW") &&
			v.Count == 3 && !v.ContainsKey("TEXTURE"));
		Assert.NotNull(vmed);

		// [MEDIUM,LIGHTNING,SIMPLESHADOW] with TEXTURE
		var vmedt = variants.FirstOrDefault(v =>
			v.ContainsKey("MEDIUM") && v.ContainsKey("LIGHTNING") && v.ContainsKey("SIMPLESHADOW") && v.ContainsKey("TEXTURE") &&
			v.Count == 4);
		Assert.NotNull(vmedt);

		// [HIGH,LIGHTNING,PCFSHADOW] without TEXTURE
		var vhigh = variants.FirstOrDefault(v =>
			v.ContainsKey("HIGH") && v.ContainsKey("LIGHTNING") && v.ContainsKey("PCFSHADOW") &&
			v.Count == 3 && !v.ContainsKey("TEXTURE"));
		Assert.NotNull(vhigh);

		// [HIGH,LIGHTNING,PCFSHADOW] with TEXTURE
		var vhight = variants.FirstOrDefault(v =>
			v.ContainsKey("HIGH") && v.ContainsKey("LIGHTNING") && v.ContainsKey("PCFSHADOW") && v.ContainsKey("TEXTURE") &&
			v.Count == 4);
		Assert.NotNull(vhight);
	}

	[Fact]
	public void TestAdvancedMixed()
	{
		var xml = _assembly.ReadResourceAsString("efscriptgen.Tests.Resources.AdvancedMixed.xml");
		var variants = VariantsParser.FromXml(xml);

		Assert.NotNull(variants);
		// 3 platform options × 2 normal map options × 3 AA options = 18 variants
		Assert.Equal(18, variants.Count);

		// Check MOBILE variants (3 with different AA, 3 without USE_NORMAL_MAP)
		var mobileVariants = variants.Where(v => v.ContainsKey("MOBILE")).ToList();
		Assert.Equal(6, mobileVariants.Count);

		var mobileNoNormal = mobileVariants.Where(v => !v.ContainsKey("USE_NORMAL_MAP")).ToList();
		Assert.Equal(3, mobileNoNormal.Count);

		var mobileWithNormal = mobileVariants.Where(v => v.ContainsKey("USE_NORMAL_MAP")).ToList();
		Assert.Equal(3, mobileWithNormal.Count);

		// Check PC_LOW variants
		var pcLowVariants = variants.Where(v => v.ContainsKey("PC_LOW")).ToList();
		Assert.Equal(6, pcLowVariants.Count);

		// Check PC_HIGH with ADVANCED_EFFECTS variants
		var pcHighVariants = variants.Where(v => v.ContainsKey("PC_HIGH") && v.ContainsKey("ADVANCED_EFFECTS")).ToList();
		Assert.Equal(6, pcHighVariants.Count);

		// Check ANTIALIASING variants - should have FXAA, SMAA, or neither
		var fxaaVariants = variants.Where(v => v.ContainsKey("ANTIALIASING") && v["ANTIALIASING"] == "FXAA").ToList();
		Assert.Equal(6, fxaaVariants.Count);

		var smaaVariants = variants.Where(v => v.ContainsKey("ANTIALIASING") && v["ANTIALIASING"] == "SMAA").ToList();
		Assert.Equal(6, smaaVariants.Count);

		var noAAVariants = variants.Where(v => !v.ContainsKey("ANTIALIASING")).ToList();
		Assert.Equal(6, noAAVariants.Count);
	}

	[Fact]
	public void TestDefaultEffect()
	{
		var xml = _assembly.ReadResourceAsString("efscriptgen.Tests.Resources.DefaultEffect.xml");
		var variants = VariantsParser.FromXml(xml);

		// Make sure all combinations exist in the variants
		for (var i = 0; i < 16; i++)
		{
			var defines = new List<string>();
			if ((i & 1) == 1)
			{
				defines.Add("TEXTURE");
			}

			if ((i & 2) == 2)
			{
				defines.Add("LIGHTNING");
			}

			if ((i & 4) == 4)
			{
				defines.Add("CLIP_PLANE");
			}

			if ((i & 8) == 8)
			{
				defines.Add("SKINNING");
			}

			Dictionary<string, string> variant = null;
			foreach (var v in variants)
			{
				if (v.Count != defines.Count)
				{
					continue;
				}
				variant = v;
				foreach (var d in defines)
				{
					if (!v.ContainsKey(d))
					{
						variant = null;
						break;
					}
				}

				if (variant != null)
				{
					break;
				}
			}

			Assert.NotNull(variant);
		}
	}

	[Fact]
	public void TestVariantValuesAreCorrect()
	{
		var xml = _assembly.ReadResourceAsString("efscriptgen.Tests.Resources.BasicVariants.xml");
		var variants = VariantsParser.FromXml(xml);

		foreach (var variant in variants)
		{
			foreach (var pair in variant)
			{
				Assert.NotNull(pair.Key);
				Assert.NotEmpty(pair.Key);
				Assert.NotNull(pair.Value);
				Assert.NotEmpty(pair.Value);
			}
		}
	}

	[Fact]
	public void TestNoEmptyDefines()
	{
		var xml = _assembly.ReadResourceAsString("efscriptgen.Tests.Resources.GroupedValues.xml");
		var variants = VariantsParser.FromXml(xml);

		foreach (var variant in variants)
		{
			foreach (var pair in variant)
			{
				// _ should never appear in the final result
				Assert.NotEqual("_", pair.Key);
			}
		}
	}

	[Fact]
	public void TestComplexGroupedValuesHaveFlatStructure()
	{
		var xml = _assembly.ReadResourceAsString("efscriptgen.Tests.Resources.GroupedValues.xml");
		var variants = VariantsParser.FromXml(xml);

		var highVariant = variants.FirstOrDefault(v =>
			v.ContainsKey("HIGH") && v.ContainsKey("LIGHTNING") && v.ContainsKey("PCFSHADOW") &&
			!v.ContainsKey("TEXTURE"));

		Assert.NotNull(highVariant);
		// Should have HIGH, LIGHTNING, and PCFSHADOW
		Assert.True(highVariant.ContainsKey("HIGH"));
		Assert.True(highVariant.ContainsKey("LIGHTNING"));
		Assert.True(highVariant.ContainsKey("PCFSHADOW"));
		// All values should be "1" since they're flags
		foreach (var pair in highVariant)
		{
			Assert.Equal("1", pair.Value);
		}
	}

	[Theory]
	[InlineData("efscriptgen.Tests.Resources.BasicVariants.xml", 4)]
	[InlineData("efscriptgen.Tests.Resources.MacrosWithValues.xml", 6)]
	[InlineData("efscriptgen.Tests.Resources.GroupedValues.xml", 6)]
	[InlineData("efscriptgen.Tests.Resources.AdvancedMixed.xml", 18)]
	[InlineData("efscriptgen.Tests.Resources.DefaultEffect.xml", 16)]
	public void TestVariantCounts(string resourceName, int expectedCount)
	{
		var xml = _assembly.ReadResourceAsString(resourceName);
		var variants = VariantsParser.FromXml(xml);

		Assert.NotNull(variants);
		Assert.Equal(expectedCount, variants.Count);
	}
	}
}
