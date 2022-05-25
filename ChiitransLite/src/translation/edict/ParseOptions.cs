using System.Collections.Generic;

namespace ChiitransLite.translation.edict
{
class ParseOptions
{
	public List<string> userWords = new();

	public void addUserWord(string w) => userWords.Add(w);

	internal int bonusRating(string word)
	{
		int pos = userWords.IndexOf(word);

		if(pos == -1)
			return 0;
		else
			return 1000000 + pos * 100;
	}
}
}
