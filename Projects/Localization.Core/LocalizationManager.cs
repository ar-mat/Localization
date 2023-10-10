using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Armat.Localization;

public class LocalizationManager
{
	private LocalizationManager()
	{
		LoggerFactory = NullLoggerFactory.Instance;
		Logger = NullLogger.Instance;

		CurrentLocale = LocaleInfo.Invalid;
	}

	public static LocalizationManager CreateDefaultInstance()
	{
		return CreateDefaultInstance(Configuration.Default, NullLoggerFactory.Instance);
	}
	public static LocalizationManager CreateDefaultInstance(Configuration config)
	{
		return CreateDefaultInstance(config, NullLoggerFactory.Instance);
	}
	public static LocalizationManager CreateDefaultInstance(ILoggerFactory loggerFactory)
	{
		return CreateDefaultInstance(Configuration.Default, loggerFactory);
	}
	public static LocalizationManager CreateDefaultInstance(Configuration config, ILoggerFactory loggerFactory)
	{
		// check if already created
		if (_default is not null)
			throw new NotSupportedException("Default Localization Manager cannot be updated");

		// create and assign to default
		_default = CreateInstance(config, loggerFactory);

		return _default;
	}
	public static LocalizationManager CreateInstance(Configuration config)
	{
		return CreateInstance(config, Default.LoggerFactory);
	}
	public static LocalizationManager CreateInstance(Configuration config, ILoggerFactory loggerFactory)
	{
		// validate input arguments
		if (loggerFactory is null)
			throw new ArgumentNullException(nameof(loggerFactory));

		// create it
		LocalizationManager result = new()
		{
			Configuration = config,

			LoggerFactory = loggerFactory,
			Logger = loggerFactory.CreateLogger<LocalizationManager>()
		};

		// set the current locale
		if (config.DefaultLocale != null)
			result.CurrentLocale = config.DefaultLocale;

		return result;
	}

	// the singleton instance
	private static LocalizationManager? _default = null;
	private static readonly LocalizationManager _none = new();
	public static LocalizationManager Default
	{
		get { return _default ?? _none; }
	}

	// Read-only instance properties
	public Configuration Configuration { get; init; }
	public ILoggerFactory LoggerFactory { get; init; }
	protected ILogger Logger { get; init; }

	// Locale related properties
	public LocaleInfo CurrentLocale { get; private set; }
	public IEnumerable<LocaleInfo> AllLocales
	{
		get
		{
			DirectoryInfo? locRootDirInfo = GetTranslationsDirectory();
			if (locRootDirInfo == null || !locRootDirInfo.Exists)
				return Array.Empty<LocaleInfo>();

			// list all locales (based on directory structure)
			List<LocaleInfo> allLocales = new();
			IEnumerable<DirectoryInfo> locDirectories = locRootDirInfo.EnumerateDirectories();
			foreach (DirectoryInfo locDirInfo in locDirectories)
			{
				// check if it contains translation files
				IEnumerable<FileInfo> translationFiles = locDirInfo.EnumerateFiles();
				if (!translationFiles.Any())
					continue;

				// check if it has a valid locale name
				LocaleInfo locale = GetLocaleInfoFromDirectory(locDirInfo);
				if (!locale.IsValid)
					continue;

				allLocales.Add(locale);
			}

			// sort to ensure consistent sequence
			IEnumerable<LocaleInfo> result = allLocales.OrderBy(loc => loc.DisplayName);

			// set the default locale first
			if (Configuration.DefaultLocale != null && !result.Contains(Configuration.DefaultLocale))
				result = result.Prepend(Configuration.DefaultLocale);

			return result;
		}
	}

	// Localization Targets (Localizable Resources)
	private readonly List<LocalizationTarget> _listTargets = new();
	private LocalizationTargetCollection? _externalTargets = null;
	public ICollection<ILocalizationTarget?> Targets
	{
		get
		{
			_externalTargets ??= new LocalizationTargetCollection(this);

			return _externalTargets;
		}
	}

	// Be careful registering delegated tp this event. It may hold a reference to your object, preventing its finalization.
	// Do not provide an event delegate from a class instance which can be disposed during the program lifetime.
	// Instead, an instance of  ILocalizationTarget can be registered to receive locale change events.
	public event LocalizationChangeEventHandler? LocalizationChanged;

	// Changing the Current Localization
	public void ChangeLocale(String localeName)
	{
		ChangeLocale(new LocaleInfo(localeName));
	}
	public void ChangeLocale(LocaleInfo locale)
	{
		if (locale == CurrentLocale)
			return;

		//if (!locale.IsValid)
		//{
		//	Logger.LogError("Cannot reset the locale to default");
		//	throw new ArgumentException("Cannot reset the locale to default", nameof(locale));
		//}

		// remember the old locale
		LocaleInfo oldLocale = CurrentLocale;

		// update the current locale
		CurrentLocale = locale;

		LocalizationChangeEventArgs changeEventArgs = new(oldLocale, locale);

		// update all Localization Targets
		for (Int32 index = 0; index < _listTargets.Count; index++)
		{
			try
			{
				// try to notify about the locale change
				if (!_listTargets[index].NotifyLocalizationChanged(this, changeEventArgs))
				{
					// coming here means that the target resource dictionary is finalized
					// remove obsolete target from the list
					_listTargets.RemoveAt(index--);
				}
			}
			catch (Exception ex)
			{
				Logger.LogError(ex, "Exception occurred when notifying about locale change");
			}
		}

		// Invoke the event
		LocalizationChanged?.Invoke(this, changeEventArgs);
	}

	// Localization Directories and Files
	public DirectoryInfo? GetTranslationsDirectory()
	{
		String locDirectoryPath = Configuration.TranslationsDirectoryPath;
		if (String.IsNullOrEmpty(locDirectoryPath))
			return null;

		if (!Path.IsPathFullyQualified(locDirectoryPath))
		{
			// combine the relative path to with the executing assembly location
			String? assemblyDir = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
			if (!String.IsNullOrEmpty(assemblyDir))
				locDirectoryPath = Path.Combine(assemblyDir, locDirectoryPath);
		}

		return new DirectoryInfo(locDirectoryPath);
	}
	public DirectoryInfo? GetTranslationsDirectory(String localeName)
	{
		if (String.IsNullOrEmpty(localeName))
			return null;

		DirectoryInfo? directoryInfo = GetTranslationsDirectory();
		if (directoryInfo == null)
			return null;

		String locDirectoryPath = Path.Combine(directoryInfo.FullName, localeName);
		return new DirectoryInfo(locDirectoryPath);
	}
	private LocaleInfo GetLocaleInfoFromDirectory(DirectoryInfo locDirInfo)
	{
		LocaleInfo result;

		try
		{
			// try to create a locale info based on the directory name
			result = new LocaleInfo(locDirInfo.Name);
			if (!result.IsValid)
				throw new CultureNotFoundException(nameof(locDirInfo), "Invalid Locale", locDirInfo.Name);
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Failed to retrieve locale info from directory name");
			result = LocaleInfo.Invalid;
		}

		return result;
	}

	#region Helper classes

	private class LocalizationTarget
	{
		private readonly WeakReference<ILocalizationTarget> _weakRef;

		public LocalizationTarget(ILocalizationTarget? target)
		{
			// do not let instantiation with null
			if (target == null)
				throw new ArgumentNullException(nameof(target));

			_weakRef = new WeakReference<ILocalizationTarget>(target);
		}
		public ILocalizationTarget? TryGetTarget()
		{
			if (_weakRef.TryGetTarget(out ILocalizationTarget? target))
				return target;

			return null;
		}

		public Boolean NotifyLocalizationChanged(LocalizationManager locManager, LocalizationChangeEventArgs args)
		{
			if (_weakRef.TryGetTarget(out ILocalizationTarget? target))
			{
				if (args.NewLocale != target.CurrentLocale)
					target.OnLocalizationChanged(locManager, args);
				return true;
			}

			return false;
		}
	}

	private class TypeConverterCollection<DataType, WrapperType> : ICollection<DataType?>
		where DataType : class?
		where WrapperType : class
	{
		private readonly ICollection<WrapperType> Container;
		private readonly Func<DataType?, WrapperType> Wrap;
		private readonly Func<WrapperType, DataType?> Extract;

		public TypeConverterCollection(ICollection<WrapperType> wrapperColl, Func<DataType?, WrapperType> wrapper, Func<WrapperType, DataType?> extractor)
		{
			Container = wrapperColl;
			Wrap = wrapper;
			Extract = extractor;
		}

		public Int32 Count => Container.Count;

		public Boolean IsReadOnly => Container.IsReadOnly;

		public virtual void Add(DataType? item)
		{
			Container.Add(Wrap(item));
		}

		public virtual Boolean Remove(DataType? item)
		{
			if (item == null)
				return false;

			foreach (WrapperType wrapper in Container)
			{
				if (Equals(Extract(wrapper), item))
					return Container.Remove(wrapper);
			}

			return false;
		}

		public virtual void Clear()
		{
			Container.Clear();
		}

		public virtual Boolean Contains(DataType? item)
		{
			if (item == null)
				return false;

			return Container.Any(wrapper => Equals(Extract(wrapper), item));
		}

		public virtual void CopyTo(DataType?[] array, Int32 arrayIndex)
		{
			foreach (WrapperType wrapper in Container)
			{
				array[arrayIndex++] = Extract(wrapper);
			}
		}

		public virtual IEnumerator<DataType?> GetEnumerator()
		{
			return Container.Select(wrapper => Extract(wrapper)).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	private class LocalizationTargetCollection : TypeConverterCollection<ILocalizationTarget?, LocalizationTarget>
	{
		private readonly LocalizationManager _locManager;

		public LocalizationTargetCollection(LocalizationManager locManager)
			: base(locManager._listTargets, Wrap, Extract)
		{
			_locManager = locManager;
		}

		public override void Add(ILocalizationTarget? item)
		{
			if (item == null)
				throw new ArgumentNullException(nameof(item));

			// add the item
			base.Add(item);

			try
			{
				// ensure to apply the localization
				LocaleInfo? currentLocale = _locManager.CurrentLocale;
				if (currentLocale != null && currentLocale != item.CurrentLocale)
					item.OnLocalizationChanged(_locManager, new LocalizationChangeEventArgs(currentLocale));
			}
			catch (Exception ex)
			{
				_locManager.Logger.LogError(ex, "Exception occurred when notifying about locale change");
			}
		}

		// wrapper method
		private static LocalizationTarget Wrap(ILocalizationTarget? target)
		{
			return new LocalizationTarget(target);
		}
		// extractor method
		private static ILocalizationTarget? Extract(LocalizationTarget wrapper)
		{
			return wrapper.TryGetTarget();
		}
	}

	#endregion // Helper Classes
}
