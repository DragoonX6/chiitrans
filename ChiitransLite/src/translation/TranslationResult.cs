namespace ChiitransLite.translation
{
class TranslationResult
{
	public readonly bool isAtlas;
	public readonly string text;

	public TranslationResult(string text, bool isAtlas)
	{
		this.text = text;
		this.isAtlas = isAtlas;
	}
}
}
