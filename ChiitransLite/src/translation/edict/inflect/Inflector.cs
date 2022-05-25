using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text.Json;

using ChiitransLite.settings;
using ChiitransLite.misc;

namespace ChiitransLite.translation.edict.inflect
{
class Inflector
{
	Dictionary<string, ConjugationsJson> conjugations;
	Dictionary<string, InflectionTrie> index;

	public static ISet<string> knownPOS
	{
		get; private set;
	}

	internal void load()
	{
		try
		{
			conjugations = new();
			knownPOS = new HashSet<string>();

			string jsonRaw = File.ReadAllText(Settings.app.ConjugationsPath);
			IList data = JsonSerializer.Deserialize<IList>(jsonRaw);

			foreach(JsonElement it in data)
			{
				List<ConjugationsVariantJson> tenses = new();
				JsonElement.ArrayEnumerator jsonTenses =
					it.GetProperty("Tenses").EnumerateArray();

				foreach(JsonElement form in jsonTenses)
				{
					JsonElement nextType;
					bool hasNextType = form.TryGetProperty(
						"Next Type",
						out nextType);

					ConjugationsVariantJson conjVar = new ConjugationsVariantJson
					{
						Formal = form.GetProperty("Formal").GetBoolean(),
						Negative = form.GetProperty("Negative").GetBoolean(),
						Suffix = form.GetProperty("Suffix").GetString(),
						Tense = form.GetProperty("Tense").GetString(),
						NextType = hasNextType
							? nextType.GetString()
							: (form.GetProperty("Tense").GetString() == "Te-form"
							   ? "te-form"
							   : null),
						Ignore = form.TryGetProperty("Ignore", out _)
					};

					/* conjVar.Ignore = conjVar.Ignore
						|| (conjVar.Tense == "Stem" && conjVar.Suffix == ""); */

					tenses.Add(conjVar);
				}

				ConjugationsJson conj = new ConjugationsJson
				{
					Name = it.GetProperty("Name").GetString(),
					PartOfSpeech = it.GetProperty("Part of Speech").GetString(),
					Tenses = tenses
				};

				conj.addBaseFormSuffix(tenses[0].Suffix);

				ConjugationsJson old;

				if(conjugations.TryGetValue(conj.Name, out old))
				{
					old.addTenses(conj.Tenses);
					old.addBaseFormSuffix(tenses[0].Suffix);
				}
				else
				{
					knownPOS.Add(conj.Name);
					conjugations.Add(conj.Name, conj);
				}
			}

			rebuildIndex();
		}
		catch(Exception ex)
		{
			Logger.logException(ex);
		}
	}

	private void rebuildIndex()
	{
		index = new();

		foreach(ConjugationsJson conj in conjugations.Values)
		{
			InflectionTrie trie = new();

			foreach(ConjugationsVariantJson form in conj.Tenses)
				trie.addForm(form, conjugations);

			index[conj.Name] = trie;
		}
	}

	internal IEnumerable<InflectionState> findMatching(
		bool canUseKatakana,
		List<string> POS,
		string text,
		int position)
	{
		List<InflectionState> results = new();
		List<Tuple<InflectionTrie, InflectionState, string>> cur = new();

		//Logger.log("Finding inflections in: " + text.Substring(position));

		foreach(string pos in POS)
		{
			InflectionTrie trie;

			if(index.TryGetValue(pos, out trie))
				cur.Add(
					Tuple.Create<InflectionTrie, InflectionState, string>(
						trie,
						null,
						pos));
		}

		int offset = 0;
		bool hasEmptySuf = false;

		while(cur.Count > 0 && position + offset <= text.Length)
		{
			/* Logger.log(
				"POS List: "
				+ string.Join(", ", cur.Select((q) => q.Item3))); */

			List<Tuple<InflectionTrie, InflectionState, string>>
				added = new(), added2 = new(), next = new();

			HashSet<string> addedPOS = new();

			foreach(Tuple<InflectionTrie, InflectionState, string> it in cur)
			{
				foreach(ConjugationsVariantJson link in it.Item1.linkForms)
				{
					if(addedPOS.Add(link.NextType))
					{
						InflectionState linked = new(
							"",
							link,
							it.Item2 == null ? null : it.Item2.tense);

						added.Add(
							Tuple.Create(
								index[link.NextType],
								linked,
								it.Item3));

						//Logger.log("Added: " + link.NextType);
					}
				}
			}

			foreach(Tuple<InflectionTrie, InflectionState, string> it in added)
			{
				foreach(ConjugationsVariantJson link in it.Item1.linkForms)
				{
					if(addedPOS.Add(link.NextType))
					{
						InflectionState linked = new(
							"",
							link,
							it.Item2 == null ? null : it.Item2.tense);

						added2.Add(
							Tuple.Create(
								index[link.NextType],
								linked,
								it.Item3));

						//Logger.log("Added2: " + link.NextType);
					}
				}
			}

			InflectionState newState = null;
			char c;

			if(position + offset < text.Length)
			{
				c = text[position + offset];

				if(canUseKatakana)
					c = TextUtils.katakanaToHiraganaChar(c);
			}
			else
				c = '\0';

			foreach(Tuple<InflectionTrie, InflectionState, string> it
					in cur.Concat(added).Concat(added2))
			{
				foreach(ConjugationsVariantJson form in it.Item1.forms)
				{
					//Logger.log("Got form: " + form.Suffix + " (" + it.Item3 + ")");
					if(newState == null)
						newState = new(
							text.Substring(position, offset),
							form,
							it.Item2 == null ? null : it.Item2.tense);
					else
						newState.updateTense(form.Tense);

					newState.addPOS(it.Item3);
				}

				InflectionTrie nextTrie;

				if(it.Item1.children.TryGetValue(c, out nextTrie))
				{
					//Logger.log(it.Item3 + ": going deeper to " + c);
					next.Add(Tuple.Create(nextTrie, it.Item2, it.Item3));
				}
			}

			if(newState != null)
			{
				if(!newState.suffix.EndsWith("てい")
					&& !newState.suffix.EndsWith("でい")
					&& !newState.suffix.EndsWith("るた"))
				{
					// dirty HACK. bad bad me :(
					if(newState.suffix == "")
						hasEmptySuf = true;

					results.Add(newState);
				}
			}

			cur = next;
			offset += 1;
		}

		if(!hasEmptySuf && (POS == null || !knownPOS.IsSupersetOf(POS)))
		{
			InflectionState state = new("");
			results.Add(state);
		}

		return results;
	}

	internal string getStem(string baseForm, IEnumerable<string> POS)
	{
		foreach(string pos in POS)
		{
			ConjugationsJson conj;

			if(conjugations.TryGetValue(pos, out conj))
			{
				foreach(string suffix in conj.BaseFormSuffixes)
				{
					if(baseForm.EndsWith(suffix))
					{
						string res = baseForm.Substring(
							0,
							baseForm.Length - suffix.Length);

						/* if (res.Length == 0)
							Logger.log(baseForm); */

						return res;
					}
				}

				return baseForm;
			}
		}

		return baseForm;
	}

	internal IEnumerable<string> getAllSuffixes(IEnumerable<string> POS)
	{
		HashSet<string> suffixes = new();

		foreach(string aPOS in POS)
		{
			ConjugationsJson conj = conjugations.GetOrDefault(aPOS);

			if(conj != null)
				suffixes.UnionWith(conj.Tenses.Select((t) => t.Suffix));
		}

		return suffixes;
	}
}
}
