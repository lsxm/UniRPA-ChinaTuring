﻿using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;
using System;
using System.Activities;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Plugins.Shared.Library.Nuget
{
    public class NuGetPackageController
    {
        public NuGetPackageController()
        {
            if (!System.IO.Directory.Exists(GlobalPackagesFolder))
            {
                System.IO.Directory.CreateDirectory(GlobalPackagesFolder);
            }

            if (!System.IO.Directory.Exists(PackagesInstallFolder))
            {
                System.IO.Directory.CreateDirectory(PackagesInstallFolder);
            }

            if (!System.IO.Directory.Exists(TargetFolder))
            {
                System.IO.Directory.CreateDirectory(TargetFolder);
            }
        }

        private static NuGetPackageController _instance = null;
        public static NuGetPackageController Instance
        {
            get
            {
                if (_instance == null) _instance = new NuGetPackageController();
                return _instance;
            }
        }

        /// <summary>
        /// AvailableActivities.xml 配置资源实例（单例）
        /// </summary>
        private XmlDocument _availableActivitiesXmlConfigResourceInstance = null;
        public XmlDocument AvailableActivitiesXmlConfigResourceInstance
        {
            get
            {
                if (_availableActivitiesXmlConfigResourceInstance == null)
                {
                    _availableActivitiesXmlConfigResourceInstance = new XmlDocument();
                    using (var ms = new MemoryStream(Plugins.Shared.Library.Properties.Resources.AvailableActivities))
                    {
                        ms.Flush();
                        ms.Position = 0;
                        _availableActivitiesXmlConfigResourceInstance.Load(ms);
                        ms.Close();
                    }
                }

                return _availableActivitiesXmlConfigResourceInstance;
            }
            set
            {
                _availableActivitiesXmlConfigResourceInstance = value;
            }
        }

        private string _availableActivitiesXmlPath = null;
        private string AvailableActivitiesXmlPath
        {
            get
            {
                if (string.IsNullOrEmpty(_availableActivitiesXmlPath))
                {
                    _availableActivitiesXmlPath = this.LocalRPAStudioDir + @"\Config\AvailableActivities.xml";
                }

                if (!File.Exists(_availableActivitiesXmlPath))
                {
                    AvailableActivitiesXmlConfigResourceInstance.Save(_availableActivitiesXmlPath);
                }

                return _availableActivitiesXmlPath;
            }
        }

        private string _localRPAStudioDir = null;
        private string LocalRPAStudioDir
        {
            get
            {
                if (_localRPAStudioDir == null)
                {
                    _localRPAStudioDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData) + @"\UniStudio";
                }

                return _localRPAStudioDir;
            }
        }

        private static ILogger _logger = null;
        public static ILogger Logger
        {
            get
            {
                if (_logger == null) _logger = NuGetPackageControllerLogger.Instance;
                return _logger;
            }
        }

        public NuGetFramework _nuGetFramework = null;
        public NuGetFramework NuGetFramework
        {
            get
            {
                if (_nuGetFramework == null) _nuGetFramework = NuGetFramework.ParseFolder("net452");
                return _nuGetFramework;
            }
        }

        public NuGet.Configuration.ISettings _defaultSettings = null;
        public NuGet.Configuration.ISettings DefaultSettings
        {
            get
            {
                //var nuGetConfigRoot = Environment.CurrentDirectory;
                var nuGetConfigRoot = Path.Combine(LocalRPAStudioDir, "Config");
                var nuGetConfigFileName = "Nuget.Default.Config";

                try
                {
                    var nuGetPath = Path.Combine(nuGetConfigRoot, nuGetConfigFileName);

                    if (!File.Exists(nuGetPath))
                    {
                        StreamWriter sw = new StreamWriter(nuGetPath);
                        sw.Write(Properties.Resources.NuGet_Default);
                        sw.Flush();
                        sw.Close();
                    }

                    if (_defaultSettings == null)
                    {
                        _defaultSettings = NuGet.Configuration.Settings.LoadSpecificSettings(nuGetConfigRoot, nuGetConfigFileName);
                    }
                    return _defaultSettings;
                }
                catch (Exception err)
                {
                    SharedObject.Instance.Output(SharedObject.OutputType.Error, $"读取当前目录下的{nuGetConfigFileName}配置文件出错", err);
                    return null;
                }
            }
        }

        public NuGet.Configuration.ISettings _userSettings = null;
        public NuGet.Configuration.ISettings UserSettings
        {
            get
            {
                //var nuGetConfigRoot = Environment.CurrentDirectory;
                var nuGetConfigRoot = Path.Combine(LocalRPAStudioDir, "Config");
                var nuGetConfigFileName = "Nuget.User.Config";

                try
                {
                    var nuGetPath = Path.Combine(nuGetConfigRoot, nuGetConfigFileName);

                    if (!File.Exists(nuGetPath))
                    {
                        StreamWriter sw = new StreamWriter(nuGetPath);
                        sw.Write(Properties.Resources.NuGet_User);
                        sw.Flush();
                        sw.Close();
                    }

                    //if (_userSettings == null) _userSettings = NuGet.Configuration.Settings.LoadSpecificSettings(System.Environment.CurrentDirectory, nugetConfigFile);
                    //return _userSettings;
                    return NuGet.Configuration.Settings.LoadSpecificSettings(nuGetConfigRoot, nuGetConfigFileName);
                }
                catch (Exception err)
                {
                    SharedObject.Instance.Output(SharedObject.OutputType.Error, $"读取当前目录下的{nuGetConfigFileName}配置文件出错", err);
                    return null;
                }
            }
        }

        /// <summary>
        /// 不缓存，因为有可能中间包源被禁用
        /// </summary>
        public SourceRepositoryProvider SourceRepositoryProvider
        {
            get
            {
                var psp = new PackageSourceProvider(DefaultSettings, null, new PackageSourceProvider(UserSettings).LoadPackageSources());
                var _sourceRepositoryProvider = new SourceRepositoryProvider(psp, Repository.Provider.GetCoreV3());

                return _sourceRepositoryProvider;
            }
        }

        private SourceRepositoryProvider _defaultSourceRepositoryProvider = null;
        public SourceRepositoryProvider DefaultSourceRepositoryProvider
        {
            get
            {
                if (_defaultSourceRepositoryProvider == null)
                {
                    var psp = new PackageSourceProvider(DefaultSettings);
                    _defaultSourceRepositoryProvider = new SourceRepositoryProvider(psp, Repository.Provider.GetCoreV3());
                }
                return _defaultSourceRepositoryProvider;
            }
        }

        private SourceRepositoryProvider _userDefineSourceRepositoryProvider = null;
        public SourceRepositoryProvider UserDefineSourceRepositoryProvider
        {
            get
            {
                if (_userDefineSourceRepositoryProvider == null)
                {
                    var psp = new PackageSourceProvider(UserSettings);
                    _userDefineSourceRepositoryProvider = new SourceRepositoryProvider(psp, Repository.Provider.GetCoreV3());
                }
                return _userDefineSourceRepositoryProvider;
            }
        }


        public string globalPackagesFolder = null;
        public string GlobalPackagesFolder
        {
            get
            {
                if (string.IsNullOrEmpty(globalPackagesFolder)) globalPackagesFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData) + @"\UniStudio\Packages\.nuget\packages";
                return globalPackagesFolder;
            }
        }

        public string _packagesInstallFolder = null;
        public string PackagesInstallFolder
        {
            get
            {
                if (string.IsNullOrEmpty(_packagesInstallFolder)) _packagesInstallFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData) + @"\UniStudio\Packages\Installed";
                return _packagesInstallFolder;
            }
        }

        public string _targetFolder = null;
        public string TargetFolder
        {
            get
            {
                if (string.IsNullOrEmpty(_targetFolder)) _targetFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData) + @"\UniStudio\Packages\Target";
                return _targetFolder;
            }
            set
            {
                _targetFolder = value;
            }
        }

        /// <summary>
        /// 搜索包含指定内容的活动，忽略大小写，只搜索前50个，用处不大
        /// </summary>
        /// <param name="searchString"></param>
        /// <returns></returns>
        public async Task<List<IPackageSearchMetadata>> Search(string searchString, bool includePrerelease, string source = "", int skip = 0, int take = 50)
        {
            var result = new List<IPackageSearchMetadata>();

            foreach (var sourceRepository in SourceRepositoryProvider.GetRepositories())
            {
                if (!string.IsNullOrEmpty(source))
                {
                    //包源过滤
                    if (sourceRepository.PackageSource.Source.ToLower() != source.ToLower())
                    {
                        continue;
                    }
                }

                var searchResource = await sourceRepository.GetResourceAsync<PackageSearchResource>();
                var supportedFramework = new[] { ".NETFramework,Version=v4.7.2" };
                var searchFilter = new SearchFilter(includePrerelease)
                {
                    SupportedFrameworks = supportedFramework,
                    IncludeDelisted = true
                };

                var jsonNugetPackages = await searchResource
                            .SearchAsync(searchString, searchFilter, skip, take, NullLogger.Instance, CancellationToken.None);

                //TODO WJF 应该根据id还是title来模糊匹配呢？？
                if (string.IsNullOrEmpty(searchString))
                {
                    foreach (var p in jsonNugetPackages)
                    {
                        var exists = result.Where(x => x.Identity.Id == p.Identity.Id).FirstOrDefault();
                        if (exists == null) result.Add(p);
                    }
                }
                else
                {
                    foreach (var p in jsonNugetPackages.Where(x => x.Title.ToLower().Contains(searchString.ToLower())))
                    {
                        var exists = result.Where(x => x.Identity.Id == p.Identity.Id).FirstOrDefault();
                        if (exists == null) result.Add(p);
                    }
                }

            }

            return result;
        }


        public async Task<List<IPackageSearchMetadata>> SearchPackageVersions(string packageid, bool includePrerelease)
        {
            var ret = new List<IPackageSearchMetadata>();
            foreach (var sourceRepository in SourceRepositoryProvider.GetRepositories())
            {
                var searchResource = await sourceRepository.GetResourceAsync<PackageMetadataResource>();

                var metadataList = await searchResource
                            .GetMetadataAsync(packageid, includePrerelease, true, NullLogger.Instance, CancellationToken.None);

                if (metadataList.Count() > ret.Count())
                {
                    ret = metadataList.ToList();
                }
            }

            return ret;
        }







        public async Task GetPackageDependencies(PackageIdentity package, SourceCacheContext cacheContext, ISet<SourcePackageDependencyInfo> availablePackages)
        {
            if (availablePackages.Contains(package)) return;

            var repositories = SourceRepositoryProvider.GetRepositories();
            foreach (var sourceRepository in repositories)
            {
                var dependencyInfoResource = await sourceRepository.GetResourceAsync<DependencyInfoResource>();
                var dependencyInfo = await dependencyInfoResource.ResolvePackage(
                    package, NuGetFramework, Logger, CancellationToken.None);
                if (dependencyInfo == null) continue;
                availablePackages.Add(dependencyInfo);
                foreach (var dependency in dependencyInfo.Dependencies)
                {
                    var identity = new PackageIdentity(dependency.Id, dependency.VersionRange.MinVersion);
                    await GetPackageDependencies(identity, cacheContext, availablePackages);
                }

                break;//只要有一个源能搜索到就不再往下搜索了
            }
        }


        public async Task DownloadAndInstall(PackageIdentity identity)
        {
            //包源靠前的地址已经找到和安装了包的时候不要再继续下面的包源操作了
            try
            {
                using (var cacheContext = new SourceCacheContext())
                {
                    var availablePackages = new HashSet<SourcePackageDependencyInfo>(PackageIdentityComparer.Default);
                    await GetPackageDependencies(identity, cacheContext, availablePackages);

                    var resolverContext = new PackageResolverContext(
                        DependencyBehavior.Lowest,
                        new[] { identity.Id },
                        Enumerable.Empty<string>(),
                        Enumerable.Empty<NuGet.Packaging.PackageReference>(),
                        Enumerable.Empty<PackageIdentity>(),
                        availablePackages,
                        SourceRepositoryProvider.GetRepositories().Select(s => s.PackageSource),
                        NullLogger.Instance);

                    var resolver = new PackageResolver();
                    var packagesToInstall = resolver.Resolve(resolverContext, CancellationToken.None)
                        .Select(p => availablePackages.Single(x => PackageIdentityComparer.Default.Equals(x, p)));
                    var packagePathResolver = new NuGet.Packaging.PackagePathResolver(PackagesInstallFolder);
                    var packageExtractionContext = new PackageExtractionContext(Logger);
                    packageExtractionContext.PackageSaveMode = PackageSaveMode.Defaultv3;
                    var frameworkReducer = new FrameworkReducer();

                    foreach (var packageToInstall in packagesToInstall)
                    {
                        // PackageReaderBase packageReader;
                        var installedPath = packagePathResolver.GetInstalledPath(packageToInstall);
                        if (installedPath == null)
                        {
                            var downloadResource = await packageToInstall.Source.GetResourceAsync<DownloadResource>(CancellationToken.None);
                            var downloadResult = await downloadResource.GetDownloadResourceResultAsync(
                                packageToInstall,
                                new PackageDownloadContext(cacheContext),
                                NuGet.Configuration.SettingsUtility.GetGlobalPackagesFolder(DefaultSettings),
                                Logger, CancellationToken.None);

                            await PackageExtractor.ExtractPackageAsync(
                                downloadResult.PackageStream,
                                packagePathResolver,
                                packageExtractionContext,
                                CancellationToken.None);
                        }

                        InstallPackage(packageToInstall);
                    }
                }
            }
            catch (Exception ex)
            {
                SharedObject.Instance.Output(SharedObject.OutputType.Error, "下载和安装nupkg包过程中出错", ex);
            }

        }


        public bool InstallPackage(PackageIdentity identity)
        {
            var packagePathResolver = new NuGet.Packaging.PackagePathResolver(PackagesInstallFolder);
            var installedPath = packagePathResolver.GetInstalledPath(identity);

            PackageReaderBase packageReader;
            packageReader = new PackageFolderReader(installedPath);
            var libItems = packageReader.GetLibItems();
            var frameworkReducer = new FrameworkReducer();
            var nearest = frameworkReducer.GetNearest(NuGetFramework, libItems.Select(x => x.TargetFramework));
            var files = libItems
                .Where(x => x.TargetFramework.Equals(nearest))
                .SelectMany(x => x.Items).ToList();
            foreach (var f in files)
            {
                InstallFile(installedPath, f);
            }

            var cont = packageReader.GetContentItems();
            nearest = frameworkReducer.GetNearest(NuGetFramework, cont.Select(x => x.TargetFramework));
            files = cont
                .Where(x => x.TargetFramework.Equals(nearest))
                .SelectMany(x => x.Items).ToList();
            foreach (var f in files)
            {
                InstallFile(installedPath, f);
            }

            try
            {
                var dependencies = packageReader.GetPackageDependencies();
                nearest = frameworkReducer.GetNearest(NuGetFramework, dependencies.Select(x => x.TargetFramework));
                foreach (var dep in dependencies.Where(x => x.TargetFramework.Equals(nearest)))
                {
                    foreach (var p in dep.Packages)
                    {
                        var local = getLocal(p.Id);
                        InstallPackage(local.Identity);
                    }
                }
            }
            catch (Exception ex)
            {
                SharedObject.Instance.Output(SharedObject.OutputType.Error, "安装nupkg包出错", ex);
            }

            if (System.IO.Directory.Exists(installedPath + @"\build"))
            {
                if (System.IO.Directory.Exists(installedPath + @"\build\x64"))
                {
                    foreach (var f in System.IO.Directory.GetFiles(installedPath + @"\build\x64"))
                    {
                        var filename = System.IO.Path.GetFileName(f);
                        var target = System.IO.Path.Combine(TargetFolder, filename);
                        CopyIfNewer(f, target);
                    }
                }
            }

            return true;
        }

        public List<Lazy<INuGetResourceProvider>> CreateResourceProviders()
        {
            var result = new List<Lazy<INuGetResourceProvider>>();
            Repository.Provider.GetCoreV3();
            return result;
        }

        private LocalPackageInfo getLocal(string identity)
        {
            FindLocalPackagesResourceV2 findLocalPackagev2 = new FindLocalPackagesResourceV2(PackagesInstallFolder);
            var packages = findLocalPackagev2.GetPackages(Logger, CancellationToken.None).ToList();
            packages = packages.Where(p => p.Identity.Id == identity).ToList();
            LocalPackageInfo res = null;
            foreach (var p in packages)
            {
                if (res == null) res = p;
                if (res.Identity.Version < p.Identity.Version) res = p;
            }
            return res;
        }

        public LocalPackageInfo GetLocalPackageInfo(PackageIdentity identity)
        {
            FindLocalPackagesResourceV2 findLocalPackagev2 = new FindLocalPackagesResourceV2(PackagesInstallFolder);
            var packages = findLocalPackagev2.GetPackages(Logger, CancellationToken.None).ToList();
            packages = packages.Where(p => p.Identity.Id == identity.Id).ToList();
            LocalPackageInfo res = null;
            foreach (var p in packages)
            {
                if (p.Identity.Version == identity.Version)
                {
                    res = p;
                    break;
                }
            }
            return res;
        }


        private void InstallFile(string installedPath, string f)
        {
            string source = "";
            string f2 = "";
            string filename = "";
            string dir = "";
            string target = "";
            try
            {
                source = System.IO.Path.Combine(installedPath, f);

                //多种情况特殊处理，如lib\xx.dll lib\net452\xx.dll content\xxx\xxx.dll等等
                var arr = f.Split('/');
                if (arr[0] == "lib")
                {
                    if (arr.Length == 2)
                    {
                        f2 = f.Substring(f.IndexOf("/", 3) + 1);
                    }
                    else
                    {
                        f2 = f.Substring(f.IndexOf("/", 4) + 1);
                    }
                }
                else
                {
                    f2 = f.Substring(f.IndexOf("/", 0) + 1);
                }

                filename = System.IO.Path.GetFileName(f2);
                dir = System.IO.Path.GetDirectoryName(f2);
                target = System.IO.Path.Combine(TargetFolder, dir, filename);
                if (!System.IO.Directory.Exists(System.IO.Path.Combine(TargetFolder, dir)))
                {
                    System.IO.Directory.CreateDirectory(System.IO.Path.Combine(TargetFolder, dir));
                }
                CopyIfNewer(source, target);

                // 根据项目配置文件 Load Activities 相关 DLL 资源
                byte[] rawAssembly = File.ReadAllBytes(target);
                Assembly assembly = Assembly.Load(rawAssembly);

                // 扫描 Activity Type，并在 AvailableActivities.xml 配置清单中追加条目
                XmlElement rootNode = AvailableActivitiesXmlConfigResourceInstance.DocumentElement;
                foreach (Type typeItem in assembly.GetTypes())
                {
                    // 扫描 Activity Type
                    if (typeItem.BaseType == typeof(CodeActivity) ||
                        typeItem.BaseType == typeof(AsyncCodeActivity) ||
                        typeItem.BaseType == typeof(NativeActivity) ||
                        typeItem.BaseType == typeof(Activity))
                    {
                        string[] groupTree = typeItem.FullName.Replace(" ", "").Split('.');
                        string typeOfStr = typeItem.FullName + "," + assembly.GetName().Name;

                        // 在 AvailableActivities.xml 配置清单中追加条目
                        AddAvailableItem(rootNode, groupTree, typeOfStr);

                        // 序列化到 Xml 文件（%LocalAppData%\UniStudio\Config\AvailableActivities.xml）
                        AvailableActivitiesXmlConfigResourceInstance.Save(AvailableActivitiesXmlPath);
                    }
                }
            }
            catch (Exception ex)
            {
                SharedObject.Instance.Output(SharedObject.OutputType.Error, "安装nupkg包文件时出错", ex);
            }
        }


        private void CopyIfNewer(string source, string target)
        {
            var infoOld = new System.IO.FileInfo(source);
            var infoNew = new System.IO.FileInfo(target);
            if (infoNew.LastWriteTime != infoOld.LastWriteTime)
            {
                try
                {
                    System.IO.File.Copy(source, target, true);
                    return;
                }
                catch (Exception)
                {

                }
                System.IO.File.Copy(source, target, true);
            }
        }

        /// <summary>
        /// 从 DLL 中扫描到的 Activity Type 资源，追加到清单中
        /// </summary>
        /// <param name="GroupTree"></param>
        /// <param name="TypeOf"></param>
        private void AddAvailableItem(XmlElement node, string[] groupTree, string typeOfStr)
        {
            if (groupTree.Length == 1)
            {
                // 叶子节点，添加 Activity
                XmlElement activityElement = AvailableActivitiesXmlConfigResourceInstance.CreateElement("Activity");
                activityElement.SetAttribute("Name", groupTree[0]);
                activityElement.SetAttribute("TypeOf", typeOfStr);
                //activityElement.SetAttribute("Icon", "");
                //activityElement.SetAttribute("ToolTip", "");
                node.AppendChild(activityElement);
                return;
            }

            XmlElement groupNode = FindGroup(node, groupTree[0]);
            if (groupNode != null)
            {
                // 分组已存在，继续向下
                List<string> newGroupTreeList = groupTree.ToList();
                newGroupTreeList.Remove(groupTree[0]);
                AddAvailableItem(groupNode, newGroupTreeList.ToArray(), typeOfStr);
            }
            else
            {
                // 分组不存在，则创建
                groupNode = AvailableActivitiesXmlConfigResourceInstance.CreateElement("Group");
                groupNode.SetAttribute("Name", groupTree[0]);
                //groupElement.SetAttribute("ToolTip", "");
                node.AppendChild(groupNode);

                List<string> newGroupTreeList = groupTree.ToList();
                newGroupTreeList.Remove(groupTree[0]);
                AddAvailableItem(groupNode, newGroupTreeList.ToArray(), typeOfStr);
            }
        }

        /// <summary>
        /// 查找指定名称的分组节点
        /// </summary>
        /// <param name="node"></param>
        /// <param name="groupName"></param>
        /// <returns></returns>
        private XmlElement FindGroup(XmlElement node, string groupName)
        {
            XmlNodeList groupNodes = node.SelectNodes("Group");
            foreach (XmlElement groupNode in groupNodes)
            {
                if (groupNode.GetAttribute("Name").Equals(groupName))
                {
                    return groupNode;
                }
            }

            // 未找到指定名称的分组
            return null;
        }


        public NuspecReader GetNuspecReaderInPackagesInstallFolder(PackageIdentity identity)
        {
            var packagePathResolver = new NuGet.Packaging.PackagePathResolver(PackagesInstallFolder);
            var installedPath = packagePathResolver.GetInstalledPath(identity);

            PackageReaderBase packageReader;
            packageReader = new PackageFolderReader(installedPath);

            return packageReader.NuspecReader;
        }



    }
}
