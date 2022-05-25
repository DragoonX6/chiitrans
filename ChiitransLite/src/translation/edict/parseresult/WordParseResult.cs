using System;
using System.Collections.Generic;
using System.Linq;

using ChiitransLite.misc;
using ChiitransLite.settings;
using ChiitransLite.translation.edict.inflect;

namespace ChiitransLite.translation.edict.parseresult
{
class WordParseResult: ParseResult
{
	private readonly string originalText;

	private EdictMatchWithType match
	{
		get => matches[0].Item1;
	}

	private InflectionState inf
	{
		get => matches[0].Item2;
	}

	private readonly List
		<
			Tuple<EdictMatchWithType, InflectionState, double>
		> matches = new();
	private static readonly Comparison
		<
			Tuple<EdictMatchWithType, InflectionState, double>
		> sortByDescendingScore = new((t1, t2) => t2.Item3.CompareTo(t1.Item3));

	public WordParseResult(
		string originalText,
		EdictMatchWithType match,
		InflectionState inf,
		double score)
	{
		this.originalText = originalText;
		this.addMatch(match, inf, score);
	}

	public void addMatch(
		EdictMatchWithType match,
		InflectionState inf,
		double score)
	{
		this.matches.Add(Tuple.Create(match, inf, score));
		this.matches.Sort(sortByDescendingScore);
	}

	public override int length
	{
		get => originalText.Length;
	}

	public override ParseResultType type
	{
		get => ParseResultType.WORD;
	}

	public override string asText() => originalText;

	internal override object serialize(OkuriganaType okType) => new object[]
	{
		originalText, getMatchStem(), getReading(okType), isName()
	};

	public string getReading(OkuriganaType okType)
	{
		string reading;

		switch(okType)
		{
		case OkuriganaType.NONE:
		{
			reading = null;
		} break;
		case OkuriganaType.NORMAL:
		{
			reading = getStemReading();
		} break;
		case OkuriganaType.ENGLISH:
		{
			if(isName())
				reading = getSelectedEntry().getNameReading();
			else
			{
				string origReading =
					(getStemReading() ?? getMatchStem()) + inf.getReading();

				reading = TextUtils.kanaToRomaji(origReading);
			}
		} break;
		case OkuriganaType.RUSSIAN:
		{
			if(isName())
				reading = getSelectedEntry().getNameReading();
			else
			{
				string origReading =
					(getStemReading() ?? getMatchStem()) + inf.getReading();

				reading = TextUtils.kanaToCyrillic(origReading);
			}
		} break;
		default:
			throw new MyException("OkuriganaType");
		}

		return reading;
	}

	public string getStemReading()
	{
		string stem = match.match.stem;

		if(string.IsNullOrEmpty(stem))
			return null;

		if(match.matchType == EdictMatchType.KANJI)
		{
			EdictEntry entry = getSelectedEntry();

			if(entry == null)
				return null;

			// hack :(
			string suffix = null;

			if(entry.kanji.Count > 0)
			{
				foreach(DictionaryKey k in entry.kanji)
				{
					if(k.text.StartsWith(stem))
					{
						string newSuffix = k.text.Substring(stem.Length);

						if(suffix == null || newSuffix.Length < suffix.Length)
							suffix = newSuffix;
					}
				}

				if(suffix == null)
					suffix = "";

				suffix = InflectionState.getReading(suffix);

				string kanaStem = null;
				string userReading = (Settings.app.getSelectedReading(stem));

				if(userReading != null && userReading.EndsWith(suffix))
				{
					kanaStem = userReading.Substring(
						0,
						userReading.Length - suffix.Length);
				}
				else
				{
					foreach(DictionaryKey k in entry.kana)
					{
						if(k.text.EndsWith(suffix))
						{
							kanaStem = k.text.Substring(
								0,
								k.text.Length - suffix.Length);

							break;
						}
					}
				}

				if(kanaStem == null)
					return null;

				return kanaStem;
			}
			else
				return null;
		}
		else
			return null;
	}

	internal EdictEntry getSelectedEntry()
	{
		int pageNum = Settings.app.getSelectedPage(getMatchStem());
		List<Tuple<Int32, RatedEntry>> entries = getRatedEntries().ToList();

		if(entries.Count == 0)
			return null;

		EdictEntry entry = null;

		if(pageNum != -1)
		{
			Tuple<Int32, RatedEntry> tt =
				entries.FirstOrDefault((t) => t.Item1 == pageNum);

			if(tt != null)
				entry = tt.Item2.entry;
		}

		if(entry == null)
			entry = entries.First().Item2.entry;

		return entry;
	}

	private IEnumerable<Tuple<int, RatedEntry>> getRatedEntries()
	{
		bool found = false;
		int pageStart = 0;

		foreach(Tuple<EdictMatchWithType, InflectionState, double> add in matches)
		{
			EdictMatchWithType match2 = add.Item1;
			InflectionState inf2 = add.Item2;

			IEnumerable<Tuple<int, RatedEntry>> it =
				match2.match.getRatedEntriesWithPageNumber(
					inf2.POS,
					inf2.suffix.Length == 0,
					false);

			foreach(Tuple<Int32, RatedEntry> t in it)
			{
				found = true;

				yield return Tuple.Create(t.Item1 + pageStart, t.Item2);
			}

			pageStart += 1000;
		}

		if(!found)
		{
			pageStart = 0;

			foreach(Tuple<EdictMatchWithType, InflectionState, double> add in matches)
			{
				EdictMatchWithType match2 = add.Item1;
				InflectionState inf2 = add.Item2;

				IEnumerable<Tuple<int, RatedEntry>> it =
					match2.match.getRatedEntriesWithPageNumber(
						inf2.POS,
						inf2.suffix.Length == 0,
						true);

				foreach(Tuple<Int32, RatedEntry> t in it)
					yield return Tuple.Create(t.Item1 + pageStart, t.Item2);

				pageStart += 1000;
			}
		}
	}

	internal override object serializeFull()
	{
		return new
		{
			stem   = match.match.stem,
			word   = serializeMatch(),
			inf    = serializeInf(),
			isName = isName()
		};
	}

	private object serializeMatch()
	{
		return from re in getRatedEntries() select serializeEntry(
			re.Item1,
			re.Item2.entry);
	}

	private object serializeEntry(int pageNum, EdictEntry entry)
	{
		return new
		{
			pageNum = pageNum,
			kanji = (from k in entry.kanji select serializeDictKey(k)),
			kana = (from k in entry.kana select serializeDictKey(k)),
			sense = (from s in entry.sense select serializeSense(s)),
			POS = entry.POS,
			nameType = entry.nameType,
			priority = entry.priority
		};
	}

	private object serializeDictKey(DictionaryKey k)
	{
		return new
		{
			text = k.text,
			misc = k.misc
		};
	}

	private object serializeSense(DictionarySense s)
	{
		return new
		{
			glossary = s.glossary,
			misc = s.misc
		};
	}

	private object serializeInf()
	{
		return new
		{
			isFormal = inf.isFormal,
			isNegative = inf.isNegative,
			tense = inf.tense
		};
	}

	internal string getMatchStem() => match.match.stem;

	/* internal EdictEntry getEntry(int pageNum)
	{
		List<RatedEntry> entries = match.match.entries;

		if (pageNum >= 0 && pageNum < entries.Count)
			return entries[pageNum].entry;
		else
			return null;
	} */

	public override IEnumerable<ParseResult> getParts() =>
		Enumerable.Repeat(this, 1);

	internal bool isName()
	{
		EdictEntry entry = getSelectedEntry();

		if(entry == null)
			return false;

		return entry.POS.Contains("name");
	}

	internal bool isReplacement()
	{
		EdictEntry entry = getSelectedEntry();

		if(entry == null)
			return false;

		return entry.POS.Contains("name") || entry.nameType == "notname";
	}

	/* internal EdictEntry findAnyName()
	{
		EdictEntry entry = getSelectedEntry();

		if (entry == null)
			return null;

		if (entry.POS.Contains("name"))
			return entry;

		foreach (var e in getEntries())
			if (e.POS.Contains("name"))
				return e;

		return null;
	} */

	internal override EdictMatchType? getMatchTypeOf(string text)
	{
		if(originalText == text)
			return match.matchType;
		else
			return null;
	}

	internal IEnumerable<EdictEntry> getEntries() =>
		from t in getRatedEntries() select t.Item2.entry;
}
}
