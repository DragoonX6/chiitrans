using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using ChiitransLite.misc;
using ChiitransLite.texthook;
using ChiitransLite.texthook.ext;
using ChiitransLite.translation.edict;

namespace ChiitransLite.settings
{
class SessionSettings
{

	private static object classLock = new();

	private static Dictionary<string, SessionSettings> cache = new();

	internal readonly string key;
	private readonly string fileName;
	public readonly string processExe;

	public Dictionary<string, EdictMatch> userNames
	{
		get; private set;
	}

	private Dictionary<long, bool> contextsEnabled;

	private List<UserHook> userHooks;

	private bool isDirty;

	private string _po;

	public string po
	{
		get => _po;
		set
		{
			if(_po != value)
			{
				_po = value;
				isDirty = true;
			}
		}
	}

	private TimeSpan _sentenceDelay;

	public TimeSpan sentenceDelay
	{
		get => _sentenceDelay;
		set
		{
			if(_sentenceDelay != value)
			{
				_sentenceDelay = value;
				isDirty = true;
			}
		}
	}

	internal static SessionSettings get(string key, string processExe = null)
	{
		lock(classLock)
		{
			SessionSettings res;

			if(!cache.TryGetValue(key, out res))
			{
				res = new(key, processExe);
				res.tryLoad();
				cache[key] = res;
			}

			return res;
		}
	}

	internal static SessionSettings getByExeName(string exeName) =>
		get(getKey(exeName), exeName);

	internal static SessionSettings getDefault() => get("default");

	internal static void saveAll()
	{
		lock(classLock)
			foreach(SessionSettings sett in cache.Values)
				sett.save();
	}

	private static string getKey(string exeName)
	{
		string exeNameUnrooted = Path.Combine(
			Path.GetDirectoryName(exeName),
			Path.GetFileName(exeName));

		string[] parts = exeNameUnrooted.Split('\\');

		if(parts.Length > 2)
			return string.Join(
				"!",
				parts[parts.Length - 2],
				parts[parts.Length - 1]);
		else
			return exeNameUnrooted.Replace('\\', '!');
	}

	public SessionSettings(string key, string processExe)
	{
		this.key = key;
		this.processExe = processExe;

		fileName = Path.Combine(Utils.getAppDataPath(), key + ".json");
		userNames = new();

		contextsEnabled = new();

		userHooks = new();

		isDirty = false;
		_po = null;

		_sentenceDelay = TimeSpan.FromMilliseconds(100);
	}

	private void tryLoad()
	{
		lock(this)
		{
			try
			{
				if(File.Exists(fileName))
				{
					string json = File.ReadAllText(fileName);
					IDictionary data = JsonSerializer.Deserialize<IDictionary>(json);

					userNames.Clear();
					IList namesJson = data["names"] as IList;

					foreach(IDictionary nameData in namesJson.Cast<IDictionary>())
					{
						string key = nameData["key"] as string;
						string sense = nameData["sense"] as string;
						string nameType = nameData["type"] as string;

						addUserName(key, sense, nameType);

					}

					contextsEnabled.Clear();
					IDictionary contextsJson = data["contexts"] as IDictionary;

					foreach(DictionaryEntry kv in contextsJson)
						contextsEnabled[long.Parse((string)kv.Key)] = (bool)kv.Value;

					_newContextsBehavior =
						(MyContextFactory.NewContextsBehavior)Enum.Parse(
							typeof(MyContextFactory.NewContextsBehavior),
							(string)data["newContexts"]);

					userHooks.Clear();
					IList hooksJson = data["hooks"] as IList;

					foreach(string code in hooksJson.Cast<string>())
					{
						UserHook hook = UserHook.fromCode(code);

						if(hook != null)
							userHooks.Add(hook);
					}

					_po = data["po"] as string;

					if(data["sentenceDelay"] != null)
					{
						int sd = (int)data["sentenceDelay"];
						_sentenceDelay = TimeSpan.FromMilliseconds(sd);
					}
				}
			}
			catch(Exception e)
			{
				Logger.logException(e);
			}

			isDirty = false;
		}
	}

	public void loadNames(IEnumerable<object> names)
	{
		lock(this)
		{
			userNames.Clear();

			foreach(dynamic name in names)
				addUserName(name.key, name.sense, name.type);

			isDirty = true;
		}
	}

	public void addUserName(string key, string sense, string nameType)
	{
		lock(this)
		{
			Settings.app.removeBannedWord(key);

			EdictMatch match = new(key);
			EdictEntryBuilder eb = new();

			eb.addKanji(new(key));
			eb.addKana(new(sense));

			DictionarySense ds = new();
			ds.addGloss(null, sense);

			eb.addSense(ds);

			if(nameType != "notname")
				eb.addPOS("name");
			else
				eb.addPOS("n");

			eb.nameType = nameType;

			match.addEntry(new RatedEntry{ entry = eb.build(), rate = 5.0F });

			userNames[key] = match;
			isDirty = true;
		}
	}

	public void removeUserName(string key)
	{
		lock(this)
		{
			userNames.Remove(key);
			isDirty = true;
		}
	}

	private void save()
	{
		lock(this)
		{
			if(isDirty)
			{
				try
				{
					object res = serialize();
					string resJson = Utils.toJson(res);
					File.WriteAllText(fileName, resJson);
				}
				catch(IOException)
				{
				}
				catch(Exception e)
				{
					Logger.logException(e);
				}

				isDirty = false;
			}
		}
	}

	internal IEnumerable<object> serializeNames()
	{
		return (from kv in userNames
				select new
				{
					key = kv.Key,
					sense = kv.Value.entries[0].entry.kana[0].text,
					type = kv.Value.entries[0].entry.nameType
				});
	}

	private object serialize()
	{
		return new
		{
			names = serializeNames(),
			contexts = contextsEnabled.ToDictionary(
				(kv) => kv.Key.ToString(), (kv) => kv.Value),
			newContexts = newContextsBehavior.ToString(),
			hooks = (from h in userHooks select h.code),
			po = _po,
			sentenceDelay = sentenceDelay.TotalMilliseconds
		};
	}

	private MyContextFactory.NewContextsBehavior _newContextsBehavior =
		MyContextFactory.NewContextsBehavior.SMART;

	public MyContextFactory.NewContextsBehavior newContextsBehavior
	{
		get => _newContextsBehavior;

		set
		{
			if(value != _newContextsBehavior)
			{
				isDirty = true;
				_newContextsBehavior = value;
			}
		}
	}

	public void setContextEnabled(int addr, int sub, bool enabled)
	{
		lock(this)
		{
			long key = contextKey(addr, sub);
			bool old;

			if(contextsEnabled.TryGetValue(key, out old))
				if(old == enabled)
					return;

			contextsEnabled[key] = enabled;
			isDirty = true;
		}
	}

	public bool tryGetContextEnabled(int addr, int sub, out bool enabled) =>
		contextsEnabled.TryGetValue(contextKey(addr, sub), out enabled);

	private long contextKey(int addr, int sub) => (((long)sub) << 32) + addr;

	internal IEnumerable<UserHook> getHookList() => userHooks;

	internal bool isHookAlreadyInstalled(UserHook userHook) =>
		userHooks.Any((h) => h.addr == userHook.addr);

	internal void addUserHook(UserHook userHook)
	{
		lock(this)
		{
			userHooks.Add(userHook);
			isDirty = true;
		}
	}

	internal bool removeUserHook(UserHook userHook)
	{
		lock(this)
		{
			bool ok = userHooks.Remove(userHook);

			if(ok)
				isDirty = true;
		}

		return isDirty;
	}

	internal void resetUserNames()
	{
		lock(this)
		{
			userNames.Clear();
			isDirty = true;
		}
	}
}
}
