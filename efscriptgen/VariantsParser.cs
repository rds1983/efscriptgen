using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace efscriptgen
{
	public static class VariantsParser
	{
		private class DefinesBuilder
		{
			private List<KeyValuePair<string, string>> _currentDefine = new List<KeyValuePair<string, string>>();
			private List<List<KeyValuePair<string, string>>> _definesByLevel;
			private int _level;
			private List<Dictionary<string, string>> _result;

			private void AddDefine(string name, string value)
			{
				_currentDefine.Add(new KeyValuePair<string, string>(name, value));
			}

			private void StoreResult()
			{
				var dict = new Dictionary<string, string>();
				foreach(var pair in _currentDefine)
				{
					dict[pair.Key] = pair.Value;
				}

				_result.Add(dict);
			}

			private void BuildInternal()
			{
				foreach (var pair in _definesByLevel[_level])
				{
					if (pair.Key != "_")
					{
						AddDefine(pair.Key, pair.Value);
					}

					if (_level < _definesByLevel.Count - 1)
					{
						++_level;
						BuildInternal();
						--_level;
					}
					else
					{
						// Store result at leafs
						StoreResult();
						if (_currentDefine.Count > 0)
						{
							_currentDefine.RemoveAt(_currentDefine.Count - 1);
						}
					}
				}
			}

			public List<Dictionary<string, string>> Build(List<List<KeyValuePair<string, string>>> definesByLevel)
			{
				_definesByLevel = definesByLevel ?? throw new ArgumentNullException(nameof(definesByLevel));
				_result = new List<Dictionary<string, string>>();
				_level = 0;

				BuildInternal();

				return _result;
			}
		}


		public static List<Dictionary<string, string>> FromXml(string xml)
		{
			var definesByLevel = new List<List<KeyValuePair<string, string>>>();

			// First run: parse data
			var xDoc = XDocument.Parse(xml);
			foreach (var multiCompile in xDoc.Root.Elements())
			{
				var parts = multiCompile.Value.Split(";");

				var levelDefine = new List<KeyValuePair<string, string>>();
				foreach (var part in parts)
				{
					var parts2 = part.Trim().Split("=");
					var key = parts2[0].Trim();

					string value = "1";
					if (parts2.Length > 1)
					{
						value = parts2[1].Trim();
					}

					levelDefine.Add(new KeyValuePair<string, string>( key, value ));
				}

				definesByLevel.Add(levelDefine);
			}

			// Second run: recursively build the result
			var definesBuilder = new DefinesBuilder();
			return definesBuilder.Build(definesByLevel);
		}
	}
}
