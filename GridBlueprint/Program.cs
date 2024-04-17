using System;
using System.IO;
using GridBlueprint.Model;
using Mars.Components.Starter;
using Mars.Interfaces.Model;

namespace GridBlueprint;

internal static class Program
{
    private static void Main()
    {
        int episodes = 1;
        // Create a new model description and add model components to it
        var description = new ModelDescription();
        description.AddLayer<GridLayer>();
        description.AddAgent<SimpleAgent, GridLayer>();
        description.AddAgent<ComplexAgent, GridLayer>();
        description.AddAgent<HelperAgent, GridLayer>();

        // Load the simulation configuration from a JSON configuration file
        var file = File.ReadAllText("config.json");
        var config = SimulationConfig.Deserialize(file);
        for (var i = 0; i < episodes; i++)
        {
            // Couple model description and simulation configuration
            var starter = SimulationStarter.Start(description, config);

            // Run the simulation
            var handle = starter.Run();

            // Close the program
            Console.WriteLine("Successfully executed iterations: " + handle.Iterations);
            starter.Dispose();    
        }
        // // Couple model description and simulation configuration
        // var starter = SimulationStarter.Start(description, config);
        //
        // // Run the simulation
        // var handle = starter.Run();
        //
        // // Close the program
        // Console.WriteLine("Successfully executed iterations: " + handle.Iterations);
        // starter.Dispose();
    }
}