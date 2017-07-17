using System;
using System.Threading.Tasks;

public static void Run(string faceForRecognition, TraceWriter log)
{
    log.Info($"C# ServiceBus topic trigger function processed message: {faceForRecognition}");
}
