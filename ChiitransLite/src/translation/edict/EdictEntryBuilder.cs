using System;
using System.Collections.Generic;
using System.Linq;

namespace ChiitransLite.translation.edict
{
class EdictEntryBuilder
{
	public List<DictionaryKeyBuilder> kanji = new();
	public List<DictionaryKeyBuilder> kana = new();

	public List<DictionarySense> sense = new();

	public List<string> POS = new();
	public ISet<string> priority = new HashSet<string>();

	public double globalMultiplier = 1.0;
	public double globalKanaMultiplier = 0.8;

	public string nameType = null;

	public void addKanji(DictionaryKeyBuilder _kanji) => kanji.Add(_kanji);

	public void addKana(DictionaryKeyBuilder _kana) => kana.Add(_kana);

	internal void addPOS(string p) => POS.Add(p);

	internal void addSense(
		DictionarySense sense,
		double globalMult = 1.0,
		double kanaMult = 0.8)
	{
		if(this.sense.Count == 0)
		{
			globalMultiplier = globalMult;
			globalKanaMultiplier = kanaMult;
		}
		else
		{
			globalMultiplier = Math.Max(globalMultiplier, globalMult);
			globalKanaMultiplier = Math.Max(globalKanaMultiplier, kanaMult);
		}

		this.sense.Add(sense);
	}

	internal EdictEntry build()
	{
		return new EdictEntry
		{
			kanji = kanji.Select((k) => k.build()).ToList(),
			kana = kana.Select((k) => k.build()).ToList(),
			sense = sense,
			POS = POS.Distinct().ToList(),
			nameType = nameType,
			priority = string.Join(", ", priority)
		};
	}

	internal void addPriority(string _priority) => priority.Add(_priority);
}
}
