using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using efscriptgen;

namespace EffectFarm
{
	enum OutputType
	{
		MGDX11,
		MGOGL,
		FNA
	}

	class Program
	{
		public static string Version
		{
			get
			{
				var assembly = typeof(Program).Assembly;
				var name = new AssemblyName(assembly.FullName);

				return name.Version.ToString();
			}
		}

		static void Log(string message)
		{
			Console.WriteLine(message);
		}

		static string OutputSubfolder(OutputType outputType)
		{
			switch (outputType)
			{
				case OutputType.MGDX11:
					return "MonoGameDX11";
				case OutputType.MGOGL:
					return "MonoGameOGL";
			}

			return "FNA";
		}

		static Dictionary<string, string> BuildScript(string inputFolder, List<string> fxFiles, OutputType outputType)
		{
			var outputFolder = Path.Combine(inputFolder, OutputSubfolder(outputType));
			if (!Directory.Exists(outputFolder))
			{
				Directory.CreateDirectory(outputFolder);
			}

			var result = new Dictionary<string, string>();
			foreach (var fx in fxFiles)
			{
				var xmlFile = Path.ChangeExtension(fx, "xml");
				var variants = new List<string>();
				if (File.Exists(xmlFile))
				{
					var variantsList = VariantsParser.FromXml(File.ReadAllText(xmlFile));
					foreach (var v in variantsList)
					{
						var variant = string.Join(";", from d in v select d.Value == "1" ? d.Key : $"{d.Key}={d.Value}");
						variants.Add(variant);
					}
				}
				else
				{
					variants.Add(string.Empty);
				}

				var sb = new StringBuilder();
				foreach (var variant in variants)
				{
					var postFix = string.Empty;
					if (!string.IsNullOrEmpty(variant))
					{
						var defines = (from d in variant.Split(";") orderby d select d.Trim()).ToArray();
						foreach (var def in defines)
						{
							var defineParts = (from d in def.Split("=") select d.Trim()).ToArray();
							foreach (var part in defineParts)
							{
								postFix += "_";
								postFix += part;
							}
						}
					}

					var name = Path.GetFileNameWithoutExtension(fx);
					var outputFile = name + postFix;
					outputFile = Path.Combine(outputFolder, Path.ChangeExtension(outputFile, "efb"));

					var commandLine = new StringBuilder();

					if (outputType != OutputType.FNA)
					{
						commandLine.Append($"mgfxc \"{fx}\" \"{outputFile}\"");
						commandLine.Append(" /Profile:");
						commandLine.Append(outputType == OutputType.MGDX11 ? "DirectX_11" : "OpenGL");

						if (!string.IsNullOrEmpty(variant))
						{
							commandLine.Append($" /Defines:{variant}");
						}
					}
					else
					{
						commandLine.Append($"fxc \"{fx}\" /Fo \"{outputFile}\"");
						commandLine.Append(" /T:fx_2_0");

						if (!string.IsNullOrEmpty(variant))
						{
							var defines = (from d in variant.Split(";") orderby d select d.Trim()).ToArray();
							foreach (var def in defines)
							{
								var defineParts = (from d in def.Split("=") select d.Trim()).ToArray();
								if (defineParts.Length == 1)
								{
									commandLine.Append($" /D {defineParts[0]}=1");
								}
								else
								{
									commandLine.Append($" /D {defineParts[0]}={defineParts[1]}");
								}
							}
						}
					}

					sb.AppendLine(commandLine.ToString());
				}

				result[Path.GetFileNameWithoutExtension(fx)] = sb.ToString();
			}

			result["all"] = string.Join(Environment.NewLine, result.Values);

			return result;
		}

		static void Write(string inputFolder, Dictionary<string, string> result, string postfix)
		{
			foreach (var pair in result)
			{
				var file = Path.Combine(inputFolder, $"compile_{pair.Key}_{postfix}.bat");
				File.WriteAllText(file, pair.Value);
			}
		}

		static void Process(string[] args)
		{
			Log($"EffectFarm script generator {Version}.");

			if (args.Length < 1)
			{
				Log("Usage: efscriptgen <folder>");
				return;
			}

			var inputFolder = args[0];
			if (!Directory.Exists(inputFolder))
			{
				Log($"Could not find '{inputFolder}'.");
				return;
			}

			var fxFiles = Directory.EnumerateFiles(inputFolder, "*.fx").ToList();
			if (fxFiles.Count == 0)
			{
				Log($"No '.fx' found at folder '{inputFolder}'.");
				return;
			}

			var script = BuildScript(inputFolder, fxFiles, OutputType.MGDX11);
			Write(inputFolder, script, "mgdx11");
			script = BuildScript(inputFolder, fxFiles, OutputType.MGOGL);
			Write(inputFolder, script, "mgogl");
			script = BuildScript(inputFolder, fxFiles, OutputType.FNA);
			Write(inputFolder, script, "fna");

			Log("The scripts generation was a success.");
		}

		static void Main(string[] args)
		{
			try
			{
				Process(args);
			}
			catch (Exception ex)
			{
				Log(ex.ToString());
			}
		}
	}
}