using System;
using System.Collections.Generic;

namespace ChiitransLite.translation.edict
{
class DictionaryKey
{
	public readonly string text;
	public readonly List<String> misc;

	public DictionaryKey(string text, List<string> misc)
	{
		this.text = text;
		this.misc = misc;
	}
}
}
