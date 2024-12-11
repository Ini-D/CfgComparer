using CfgCompLib.classes;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;

namespace CfgCompLib {
    public static class CfgFromFlowChart {
        public static Graph GenerateGraphFromXML(string xmlPath)
        {
            Graph graph = new();

            string xsdPath = Configuration.config.GetRequiredSection("Settings").GetValue<string>("XSDPath");
            if (String.IsNullOrEmpty(xsdPath)) {
                throw new FileNotFoundException("*.xsd fíle for validating flow chart xml not found");
            } 

            XmlReaderSettings settings = new(); 
            settings.Schemas.Add("", xsdPath);  //Load XSD into settings without set namespace
            settings.ValidationType = ValidationType.Schema;

            XmlReader reader = XmlReader.Create(xmlPath, settings);
            
            XmlDocument document = new();
            
            try {          
                document.Load(reader);  //Load XML document and validate with XSD from settings
            } catch (Exception ex) {
                throw new XmlSchemaValidationException("XML validation failed: " + ex.Message);
            }
            XmlNode root = document.DocumentElement;
            XmlNodeList vertices = root.SelectNodes("//*[@vertex='1']");  //go from root and get nodes,        
            XmlNodeList edges = root.SelectNodes("//*[@edge='1']");       //edges by set attribute = 1

            Dictionary<string,int> idMapping = [];
            int graphId = 0;

            foreach (XmlElement vertex in vertices) {  
                idMapping.Add(vertex.GetAttribute("id"), graphId);  //provide new IDs for nodes
                graph.AddNode(new(graphId, PrepareLabel(vertex.GetAttribute("value")))); //get label content
                graphId++;   
            };

            foreach (XmlElement edge in edges) {
                try { 

                int sourceId = idMapping[edge.GetAttribute("source")]; //create graph edges
                int targetId = idMapping[edge.GetAttribute("target")]; //by attributes "source" + "target"      
                graph.AddEdge(graph.GetNode(sourceId), graph.GetNode(targetId));

                } catch (KeyNotFoundException ex){   
                    throw new KeyNotFoundException($"Edge could not be added to the flow chart graph, because source or target with ID {ex.Message.Split("'")[1]} not vertex in the graph");
                }; 
            };

            return graph;
        }
        public static List<string> PrepareLabel(string source) {
            
            //Drawio flow charts may contain HTML elements in "value" for line breaks, style... and HTML encoding - this needs to be removed
            //also Multiline flow elements need to be separated into expression by split at ";"
            string decodedString = WebUtility.HtmlDecode(source);
            decodedString = Regex.Replace(decodedString, "<[/]?div>", ";");
            decodedString = Regex.Replace(decodedString, "<br>", ";");
            List<string> expressions = [.. decodedString.Split(";", StringSplitOptions.RemoveEmptyEntries)];
            
            for (int i=0; i<expressions.Count; i++) {
                expressions[i] = Regex.Replace(expressions[i], "<[^<.]+>", "").Trim();
                if (String.IsNullOrEmpty(expressions[i])) {
                    expressions.RemoveAt(i);
                }
            }
            return expressions;
        }
    }
}
