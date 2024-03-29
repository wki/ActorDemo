﻿// See https://aka.ms/new-console-template for more information

using HierarchicalJobRunner.Job;
using HierarchicalJobRunner.Processing;
using MinimalActorLib;

var system = new ActorSystem();

var tree = new Group
{
    Id = Guid.NewGuid(),
    Name = "Installation",
    Children = 
    [
        new DownloadJob
        {
            Id = Guid.NewGuid(),
            Name = "Download artifacts",
            DestinationPath = "/path/to/x",
            Url = new Uri("https://www.heise.de")
        },
        
        new ExecuteJob
        {
            Id = Guid.NewGuid(),
            Name = "Run installer",
            CommandLine = "/path/to/bin -v --force"
        }
    ]
};

var processor = system.ActorOf<GroupExecutor>(tree);
// system.Tell(processor, new Start());

var finished = await system.AskAsync<Finished>(processor, new Start(), 10000);
Console.WriteLine($"Job finished with RunStatus: {finished.RunStatus}");

await Task.Delay(20000);
