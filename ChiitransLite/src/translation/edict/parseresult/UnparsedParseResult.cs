using System.Collections.Generic;
using System.Linq;

namespace ChiitransLite.translation.edict.parseresult
{
class UnparsedParseResult: ParseResult
{
	public string text
	{
		get; private set;
	}

	internal UnparsedParseResult(string text) =>
		this.text = text == null ? string.Empty : text;

	public override int length
	{
		get => text.Length;
	}

	public override ParseResultType type
	{
		get => ParseResultType.UNPARSED;
	}

	public override string asText() => text;

	internal override object serializeFull() => new { text = text };

	public override IEnumerable<ParseResult> getParts() =>
		Enumerable.Repeat(this, 1);

	internal override object serialize(settings.OkuriganaType okType) => text;

	internal override EdictMatchType? getMatchTypeOf(string text) => null;
}
}
