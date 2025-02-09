using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using ChiitransLite.misc;

namespace ChiitransLite.translation
{
class ReplacementScript
{
	private class Replacement
	{
		public Regex key;
		public string replacement;

		public Replacement(string key, string replacement)
		{
			this.key = new(key);
			this.replacement = replacement;
		}
	}

	private static List<Replacement> replacements;
	private static readonly object _lock = new();

	public static ReplacementScript get()
	{
		lock(_lock)
		{
			if(replacements == null)
				loadReplacements();
		}

		return new ReplacementScript();
	}

	private static void loadReplacements()
	{
		replacements = new();
		string[] allLines = File.ReadAllLines(
			settings.Settings.app.ReplacementScriptPath);

		foreach(string ln in allLines)
		{
			if(string.IsNullOrWhiteSpace(ln) || ln.StartsWith("*"))
				continue;

			string[] parts = ln.Split('\t');

			if(parts.Length == 1)
				replacements.Add(new Replacement(parts[0], ""));
			else if(parts.Length == 2)
				replacements.Add(new Replacement(parts[0], parts[1]));
			else
				Logger.log("(TAHelper script) Ignoring line: " + ln);
		}
	}

	internal string process(string src)
	{
		string res = src;

		foreach(Replacement repl in replacements)
			res = repl.key.Replace(res, repl.replacement);

		/* if (src != res)
			Logger.log(string.Format("(TAHelper script) Fixed {0} -> {1}.", src, res)); */

		return res;
	}
}
}
