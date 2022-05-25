using System.Collections.Generic;

namespace ChiitransLite.translation.edict
{
class DictionarySense
{
	public List<string> glossary = new();
	public List<string> misc = null;

	internal void addGloss(string lang, string value)
	{
		if(string.IsNullOrEmpty(lang))
			glossary.Add(value);
	}

	internal void addMisc(string value)
	{
		if(misc == null)
			misc = new();

		misc.Add(value);
	}
}
}
