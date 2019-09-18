/*
 *
 * (c) Copyright Ascensio System Limited 2010-2018
 *
 * This program is freeware. You can redistribute it and/or modify it under the terms of the GNU 
 * General Public License (GPL) version 3 as published by the Free Software Foundation (https://www.gnu.org/copyleft/gpl.html). 
 * In accordance with Section 7(a) of the GNU GPL its Section 15 shall be amended to the effect that 
 * Ascensio System SIA expressly excludes the warranty of non-infringement of any third-party rights.
 *
 * THIS PROGRAM IS DISTRIBUTED WITHOUT ANY WARRANTY; WITHOUT EVEN THE IMPLIED WARRANTY OF MERCHANTABILITY OR
 * FITNESS FOR A PARTICULAR PURPOSE. For more details, see GNU GPL at https://www.gnu.org/copyleft/gpl.html
 *
 * You can contact Ascensio System SIA by email at sales@onlyoffice.com
 *
 * The interactive user interfaces in modified source and object code versions of ONLYOFFICE must display 
 * Appropriate Legal Notices, as required under Section 5 of the GNU GPL version 3.
 *
 * Pursuant to Section 7 § 3(b) of the GNU GPL you must retain the original ONLYOFFICE logo which contains 
 * relevant author attributions when distributing the software. If the display of the logo in its graphic 
 * form is not reasonably feasible for technical reasons, you must include the words "Powered by ONLYOFFICE" 
 * in every copy of the program you distribute. 
 * Pursuant to Section 7 § 3(e) we decline to grant you any rights under trademark law for use of our trademarks.
 *
*/


using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

using ASC.Core;
using ASC.Data.Storage.Configuration;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ASC.Data.Storage
{
    public class WebPathSettings
    {
        private readonly IEnumerable<Appender> Appenders;


        public WebPathSettings(IServiceProvider serviceProvider)
        {
            var section = serviceProvider.GetService<Configuration.Storage>();
            if (section != null)
            {
                Appenders = section.Appender;
            }
        }

        public string GetRelativePath(string absolutePath)
        {
            if (!Uri.IsWellFormedUriString(absolutePath, UriKind.Absolute))
            {
                throw new ArgumentException(string.Format("bad path format {0} is not absolute", absolutePath));
            }

            var appender = Appenders.FirstOrDefault(x => absolutePath.Contains(x.Append) || (absolutePath.Contains(x.AppendSecure) && !string.IsNullOrEmpty(x.AppendSecure)));
            if (appender == null)
            {
                return absolutePath;
            }
            return SecureHelper.IsSecure() && !string.IsNullOrEmpty(appender.AppendSecure) ?
                absolutePath.Remove(0, appender.AppendSecure.Length) :
                absolutePath.Remove(0, appender.Append.Length);
        }

        public string GetPath(string relativePath)
        {
            if (!string.IsNullOrEmpty(relativePath) && relativePath.IndexOf('~') == 0)
            {
                throw new ArgumentException(string.Format("bad path format {0} remove '~'", relativePath), "relativePath");
            }

            var result = relativePath;
            var ext = Path.GetExtension(relativePath).ToLowerInvariant();

            if (Appenders.Any())
            {
                var avaliableAppenders = Appenders.Where(x => x.Extensions != null && x.Extensions.Split('|').Contains(ext) || string.IsNullOrEmpty(ext)).ToList();
                var avaliableAppendersCount = avaliableAppenders.LongCount();

                Appender appender;
                if (avaliableAppendersCount > 1)
                {
                    appender = avaliableAppenders[(int)(relativePath.Length % avaliableAppendersCount)];
                }
                else if (avaliableAppendersCount == 1)
                {
                    appender = avaliableAppenders.First();
                }
                else
                {
                    appender = Appenders.First();
                }

                if (appender.Append.IndexOf('~') == 0)
                {
                    var query = string.Empty;
                    //Rel path
                    if (relativePath.IndexOfAny(new[] { '?', '=', '&' }) != -1)
                    {
                        //Cut it
                        query = relativePath.Substring(relativePath.IndexOf('?'));
                        relativePath = relativePath.Substring(0, relativePath.IndexOf('?'));
                    }
                    //if (HostingEnvironment.IsHosted)
                    //{
                    //    result = VirtualPathUtility.ToAbsolute(string.Format("{0}/{1}{2}", appender.Append.TrimEnd('/'), relativePath.TrimStart('/'), query));
                    //}
                    //else
                    //{
                    result = string.Format("{0}/{1}{2}", appender.Append.TrimEnd('/').TrimStart('~'), relativePath.TrimStart('/'), query);
                    //}
                }
                else
                {
                    //TODO HostingEnvironment.IsHosted
                    if (SecureHelper.IsSecure() && !string.IsNullOrEmpty(appender.AppendSecure))
                    {
                        result = string.Format("{0}/{1}", appender.AppendSecure.TrimEnd('/'), relativePath.TrimStart('/'));
                    }
                    else
                    {
                        //Append directly
                        result = string.Format("{0}/{1}", appender.Append.TrimEnd('/'), relativePath.TrimStart('/'));
                    }
                }
            }
            //To LOWER! cause Amazon is CASE SENSITIVE!
            return result.ToLowerInvariant();
        }
    }

    public class WebPath
    {
        private static readonly IDictionary<string, bool> Existing = new ConcurrentDictionary<string, bool>();

        public WebPathSettings WebPathSettings { get; }
        public StaticUploader StaticUploader { get; }
        public CdnStorageSettings CdnStorageSettings { get; }
        public IHttpContextAccessor HttpContextAccessor { get; }
        public IWebHostEnvironment WebHostEnvironment { get; }
        public CoreBaseSettings CoreBaseSettings { get; }

        public WebPath(
            WebPathSettings webPathSettings, 
            StaticUploader staticUploader, 
            CdnStorageSettings cdnStorageSettings, 
            IHttpContextAccessor httpContextAccessor,
            IWebHostEnvironment webHostEnvironment,
            CoreBaseSettings coreBaseSettings)
        {
            WebPathSettings = webPathSettings;
            StaticUploader = staticUploader;
            CdnStorageSettings = cdnStorageSettings;
            HttpContextAccessor = httpContextAccessor;
            WebHostEnvironment = webHostEnvironment;
            CoreBaseSettings = coreBaseSettings;
        }

        public string GetPath(string relativePath)
        {
            if (!string.IsNullOrEmpty(relativePath) && relativePath.IndexOf('~') == 0)
            {
                throw new ArgumentException(string.Format("bad path format {0} remove '~'", relativePath), "relativePath");
            }

            if (CoreBaseSettings.Standalone && StaticUploader.CanUpload())
            {
                try
                {
                    var result = CdnStorageSettings.Load().DataStore.GetInternalUri("", relativePath, TimeSpan.Zero, null).AbsoluteUri.ToLower();
                    if (!string.IsNullOrEmpty(result)) return result;
                }
                catch (Exception)
                {

                }
            }

            return WebPathSettings.GetPath(relativePath);
        }

        public bool Exists(string relativePath)
        {
            var path = GetPath(relativePath);
            if (!Existing.ContainsKey(path))
            {
                if (Uri.IsWellFormedUriString(path, UriKind.Relative) && HttpContextAccessor?.HttpContext != null)
                {
                    //Local
                    Existing[path] = File.Exists(Path.Combine(WebHostEnvironment.ContentRootPath, path));
                }
                if (Uri.IsWellFormedUriString(path, UriKind.Absolute))
                {
                    //Make request
                    Existing[path] = CheckWebPath(path);
                }
            }
            return Existing[path];
        }

        private bool CheckWebPath(string path)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(path);
                request.Method = "HEAD";
                using var resp = (HttpWebResponse)request.GetResponse();
                return resp.StatusCode == HttpStatusCode.OK;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}