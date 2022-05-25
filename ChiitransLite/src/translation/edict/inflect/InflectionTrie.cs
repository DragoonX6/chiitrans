using System.Collections.Generic;

namespace ChiitransLite.translation.edict.inflect
{
class InflectionTrie
{
	public List<ConjugationsVariantJson> forms = new();
	public List<ConjugationsVariantJson> linkForms = new();
	public Dictionary<char, InflectionTrie> children = new();

	private InflectionTrie get(char c)
	{
		InflectionTrie res;

		if(!children.TryGetValue(c, out res))
		{
			res = new();
			children.Add(c, res);
		}

		return res;
	}

	public void addForm(
		ConjugationsVariantJson form,
		Dictionary<string, ConjugationsJson> conjugations)
	{
		InflectionTrie cur = this;

		if(!form.Ignore)
		{
			foreach(char c in form.Suffix)
				cur = cur.get(c);

			cur.forms.Add(form);
		}

		if(form.NextType != null)
		{
			string baseSuf = getStem(conjugations, form.Suffix, form.NextType);
			cur = this;

			foreach(char c in baseSuf)
				cur = cur.get(c);

			cur.linkForms.Add(form);
		}
	}

	private static string getStem(
		Dictionary<string, ConjugationsJson> conjugations,
		string baseForm,
		string pos)
	{
		ConjugationsJson conj;

		if(conjugations.TryGetValue(pos, out conj))
		{
			foreach(string suffix in conj.BaseFormSuffixes)
			{
				if(baseForm.EndsWith(suffix))
					return baseForm.Substring(
						0,
						baseForm.Length - suffix.Length);
			}

			return baseForm;
		}
		else
			return baseForm;
	}
}
}
