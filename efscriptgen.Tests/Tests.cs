using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace efscriptgen.Tests
{
	[TestFixture]
	public class Tests
	{
		private static readonly Assembly _assembly = typeof(Tests).Assembly;

		[Test]
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

				Assert.IsTrue(variant != null, $"Could not find variant for defines '{string.Join(", ", defines)}'");
			}
		}
	}
}
