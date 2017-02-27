using System.IO;

var args = Environment.GetCommandLineArgs();

if (args.Length != 3)
{
    Console.WriteLine("Title required");
    return;
}

var title = args[2];
var escapedTitle = Escape(title);
var date = DateTime.Now.ToString("yyyy-MM-dd");
var name = date + "-" + title.ToLowerInvariant().Replace(" ", "-").Replace("<", " ").Replace(">", " ").Replace(":", "");
var postPath = "_posts/" + name + ".md";
var assetsPath = "assets/" + name + "/";
var postContents = $@"
---
title: {escapedTitle}
assets: /{assetsPath}
tags: [ ]
---
Example of referencing an asset (in this case, an image):

![Example]({{ page.assets }}example.png ""Example"")
";

Console.WriteLine("Creating post...");
File.WriteAllText(postPath, postContents);
Console.WriteLine("...done");

Console.WriteLine("Creating assets directory...");
Directory.CreateDirectory(assetsPath);
Console.WriteLine("...done");

private static string Escape(string input)
{
    return input
        .Replace("<", "&lt;")
        .Replace(">", "&gt;")
        .Replace(":", "&#58;");
}