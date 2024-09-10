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

		static void Process(string inputFolder, List<string> fxFiles, OutputType outputType)
		{
			var result = new Dictionary<string, string>();
			foreach (var fx in fxFiles)
			{
				// Build the output folder
				var outputFolder = Path.GetFullPath(inputFolder);
				outputFolder = Path.Combine(outputFolder, OutputSubfolder(outputType));
				outputFolder = Path.Combine(outputFolder, "bin");

				var subFolder = Path.GetDirectoryName(fx).Substring(inputFolder.Length);
				if (subFolder.StartsWith(Path.DirectorySeparatorChar))
				{
					subFolder = subFolder.Substring(1);
				}

				if (!string.IsNullOrEmpty(subFolder))
				{
					outputFolder = Path.Combine(outputFolder, subFolder);
				}

				if (!Directory.Exists(outputFolder))
				{
					Directory.CreateDirectory(outputFolder);
				}

				// Build variants list
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

					var fxFullPath = Path.GetFullPath(fx);
					if (outputType != OutputType.FNA)
					{
						commandLine.Append($"mgfxc \"{fxFullPath}\" \"{outputFile}\"");
						commandLine.Append(" /Profile:");
						commandLine.Append(outputType == OutputType.MGDX11 ? "DirectX_11" : "OpenGL");

						if (!string.IsNullOrEmpty(variant))
						{
							commandLine.Append($" /Defines:{variant}");
						}
					}
					else
					{
						commandLine.Append($"fxc \"{fxFullPath}\" /Fo \"{outputFile}\"");
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
					sb.AppendLine(@"@if %errorlevel% neq 0 exit /b %errorlevel%");
				}

				var id = Path.Combine(subFolder, Path.GetFileNameWithoutExtension(fx));
				id = id.Replace('\\', '_');
				id = id.Replace('/', '_');
				result[id] = sb.ToString();
			}

			result["all"] = string.Join(Environment.NewLine, result.Values);

			inputFolder = Path.Combine(inputFolder, OutputSubfolder(outputType));

			foreach (var pair in result)
			{
				var file = Path.Combine(inputFolder, $"compile_{pair.Key}.bat");
				File.WriteAllText(file, pair.Value);
			}
		}

		static void Process(string[] args)
		{
			Log($"Effect compilation script generator {Version}.");

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

			var fxFiles = Directory.EnumerateFiles(inputFolder, "*.fx",
				new EnumerationOptions { RecurseSubdirectories = true }).ToList();
			if (fxFiles.Count == 0)
			{
				Log($"No '.fx' found at folder '{inputFolder}'.");
				return;
			}

			Process(inputFolder, fxFiles, OutputType.MGDX11);
			Process(inputFolder, fxFiles, OutputType.MGOGL);
			Process(inputFolder, fxFiles, OutputType.FNA);

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