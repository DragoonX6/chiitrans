using System.Collections.Concurrent;
using System.Collections.Generic;

using ChiitransLite.settings;

namespace ChiitransLite.texthook.ext
{
class MyContext: TextHookContext
{
	const int MAX_LOG = 20;

	private bool _enabled;

	public bool enabled
	{
		get => _enabled;

		set
		{
			_enabled = value;
			Settings.session.setContextEnabled(context, subcontext, value);
		}
	}

	public ConcurrentQueue<string> log = new();

	public MyContext(
		int id,
		string name,
		int hook,
		int context,
		int subcontext,
		int status,
		bool enabled)
	: base(id, name, hook, context, subcontext, status)
	{
		this.enabled = enabled;
		onSentence += MyContext_onSentence;
	}

	void MyContext_onSentence(TextHookContext sender, string text)
	{
		log.Enqueue(text);

		if(log.Count > MAX_LOG)
		{
			string unused;
			log.TryDequeue(out unused);
		}
	}

	public IEnumerable<string> getLog() => log;
}
}
