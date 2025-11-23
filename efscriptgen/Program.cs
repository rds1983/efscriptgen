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
		private class FilePair
		{
			public string Id;
			public string Fx;
			public string Xml;

			public override string ToString() => $"Fx = {Fx}, Xml = {Xml}";
		}

		public static string Version
		{
			get
			{
				var assembly = typeof(Program).Assembly;
				var name = new AssemblyName(assembly.FullName);

				return name.Version.ToString();
			}
		}

		private static Options Options { get; } = new Options();

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

		static void Process(string inputFolder, List<FilePair> files, OutputType outputType)
		{
			var result = new Dictionary<string, string>();
			foreach (var pair in files)
			{
				// Build the output folder
				var outputFolder = Path.GetFullPath(inputFolder);
				outputFolder = Path.Combine(outputFolder, OutputSubfolder(outputType));
				outputFolder = Path.Combine(outputFolder, "bin");

				var subFolder = Path.GetDirectoryName(pair.Fx).Substring(inputFolder.Length);
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
				var xmlFile = pair.Xml;
				var variants = new List<string>();
				if (File.Exists(xmlFile))
				{
					var variantsList = VariantsParser.FromXml(File.ReadAllText(xmlFile));
					foreach (var v in variantsList)
					{
						var variant = string.Join(";", from d in v select d.Value == "1" ? d.Key : $"{d.Key}={d.Value}");
						variants.Add(variant);
					}

					pair.Id = Path.GetFileNameWithoutExtension(pair.Xml);
				}
				else
				{
					variants.Add(string.Empty);
					pair.Id = Path.GetFileNameWithoutExtension(pair.Fx);
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

					var name = Path.GetFileNameWithoutExtension(pair.Fx);
					var outputFile = name + postFix;
					outputFile = Path.Combine(outputFolder, Path.ChangeExtension(outputFile, Options.Extension));

					var commandLine = new StringBuilder();

					var fxFullPath = Path.GetFullPath(pair.Fx);
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

				var id = Path.Combine(subFolder, pair.Id);
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

		static string ParseString(string name, string[] args, ref int i)
		{
			++i;
			if (i >= args.Length)
			{
				throw new Exception($"Value isn't provided for '{name}'");
			}

			return args[i];
		}

		static void Process(string[] args)
		{
			Log($"Effect compilation script generator {Version}.");

			if (args.Length < 1)
			{
				Log("Usage: efscriptgen <folder> [options]");
				Log(string.Empty);
				Log("Options:");
				Log("-e <extension>    Specifies the compiled files extension. Default value is 'efb'.");
				return;
			}

			var inputFolder = string.Empty;

			for(var i = 0; i < args.Length; ++i)
			{
				var arg = args[i];
				if (arg.StartsWith("-"))
				{
					switch (arg)
					{
						case "-e":
							Options.Extension = ParseString("e", args, ref i);
							break;
					}
				} else
				{
					inputFolder = arg;
				}
			}

			if (string.IsNullOrEmpty(inputFolder))
			{
				throw new Exception($"Input folder isn't set");
			}

			if (!Directory.Exists(inputFolder))
			{
				Log($"Could not find '{inputFolder}'.");
				return;
			}

			var files = new List<FilePair>();
			var fxFiles = Directory.EnumerateFiles(inputFolder, "*.fx",
				new EnumerationOptions { RecurseSubdirectories = true }).ToList();

			// Add all fx files
			foreach (var fxFile in fxFiles)
			{
				var pair = new FilePair
				{
					Fx = fxFile,
					Xml = string.Empty
				};

				var xmlFile = Path.ChangeExtension(fxFile, "xml");
				if (File.Exists(xmlFile))
				{
					pair.Xml = xmlFile;
				}

				files.Add(pair);
				Log($"Added {pair}");
			}

			// Add standalone xmls
			var xmlFiles = Directory.EnumerateFiles(inputFolder, "*.xml",
				new EnumerationOptions { RecurseSubdirectories = true }).ToList();
			foreach(var xml in xmlFiles)
			{
				var fx = Path.ChangeExtension(xml, "fx");
				if (File.Exists(fx))
				{
					// Should be added already
					continue;
				}

				// Determine file from xml
				var xDoc = XDocument.Load(xml);

				if (xDoc.Root == null || xDoc.Root.Attribute("File") == null)
				{
					throw new Exception($"Standalone xml '{xml}' doesnt reference fx");
				}

				var file = xDoc.Root.Attribute("File").Value;
				var folder = Path.GetDirectoryName(xml);
				var fxFile = Path.Combine(folder, file);

				if (!File.Exists(fxFile))
				{
					throw new Exception($"Could not find references file '{fxFile}'");
				}

				var pair = new FilePair
				{
					Fx = fxFile,
					Xml = xml
				};

				files.Add(pair);
				Log($"Added {pair}");
			}

			if (fxFiles.Count == 0)
			{
				Log($"No '.fx' found at folder '{inputFolder}'.");
				return;
			}

			Process(inputFolder, files, OutputType.MGDX11);
			Process(inputFolder, files, OutputType.MGOGL);
			Process(inputFolder, files, OutputType.FNA);

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