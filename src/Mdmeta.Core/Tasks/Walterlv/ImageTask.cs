﻿using System;
using Mdmeta.Core;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using static Mdmeta.Tasks.Walterlv.MdmetaUtils;

namespace Mdmeta.Tasks.Walterlv
{
    [CommandMetadata("wimage", Description = "将文件转换为 CSDN 博客能原生支持的 Markdown 格式。")]
    public class ImageTask : CommandTask
    {
        [CommandArgument("[file]", Description = "要转换格式的文件的完全限定路径。")]
        public string FileName { get; set; }

        [CommandOption("-b|--image-base-path", Description = "图片在本地文件系统中的基地址。")]
        public string ImageBasePath { get; set; }

        public override int Run()
        {
            if (!File.Exists(FileName))
            {
                OutputError("文件不存在。");
                return 4;
            }

            var file = new FileInfo(FileName);

            var text = File.ReadAllText(file.FullName, Encoding.UTF8);
            //                           |  非 <!-- 开头 |  取 ！[ ] 部分  |      取 ( ) 部分
            var imageRegex = new Regex(@"(?<!\<\!\-\-\s?)!\[(?<name>.+)\]\((?<path>/static/posts/[\d-]+\.png)\)");
            var matches = imageRegex.Matches(text);
            int count = 0;
            foreach (Match match in matches)
            {
                var name = match.Groups["name"].Value;
                var path = match.Groups["path"].Value;

                var server = new Smms();
                var localImagePath = Path.GetFullPath(ImageBasePath + path);
                if (!File.Exists(localImagePath))
                {
                    Console.WriteLine(
                        $"{count.ToString().PadLeft(2, ' ')}. " +
                        $"{path} 已经是网络图片，无需上传。");
                    count++;
                    continue;
                }

                Console.Write(
                    $"{count.ToString().PadLeft(2, ' ')}. " +
                    $"{name} ");
                try
                {
                    Console.CursorLeft = 4;
                    var uploadedUrl = server.UploadAsync(localImagePath).Result.Url;
                    Console.WriteLine($"{path} 已上传至 {uploadedUrl} 。");

                    text = text.Replace(match.Value, $@"<!-- {match.Value} -->
{match.Value.Replace(path, uploadedUrl)}");
                }
                catch (Exception ex)
                {
                    OutputError($"{path} 上传失败：{ex.Message}");
                }

                count++;
            }

            File.WriteAllText(file.FullName, text, Encoding.UTF8);
            return 0;
        }
    }
}