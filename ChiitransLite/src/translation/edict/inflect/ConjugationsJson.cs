using System.Collections.Generic;
using System.Linq;

namespace ChiitransLite.translation.edict.inflect
{
class ConjugationsJson
{
	public string Name;
	public string PartOfSpeech;
	public List<string> BaseFormSuffixes = new();
	public List<ConjugationsVariantJson> Tenses;

	internal void addBaseFormSuffix(string p)
	{
		if(!BaseFormSuffixes.Contains(p))
			BaseFormSuffixes.Add(p);
	}

	internal void addTenses(List<ConjugationsVariantJson> newTenses)
	{
		foreach(ConjugationsVariantJson form in newTenses)
			if(!Tenses.Any((t) => t.Suffix == form.Suffix))
				Tenses.Add(form);
	}
}
}
