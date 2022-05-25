namespace ChiitransLite.texthook
{
interface ContextFactory
{
	TextHookContext create(
		int id,
		string name,
		int hook,
		int context,
		int subcontext,
		int status);

	void onConnected();
}
}
