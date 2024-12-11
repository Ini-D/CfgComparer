## C Program and Flow Chart Comparer

The CfgComparer is a .NET-library for comparing and evaluating C programs with their underlying respective flow charts. 
The analysis is based on graph algorithms comparing both as control flow graphs.

It is designed for checking programmers control and data flow understanding by evaluating manually created flow charts accurracy against the C program it was made for.    
This project was developed for the beginner programming lessons at the [Merseburg University of Applied Sciences](https://www.hs-merseburg.de/international/).

## Concept

The comparison is based on:
   * determination of the Maximum Common Connected Subgraph - if both graphs are completely inside, then C program and flow chart are completely equal
   * calculation of the Graph Edit Distance of rest nodes   - determination of transformation costs as measure of equality (the lower the closer to equality)
   * the label content (aka program expressions) is compared on a metric based on string, syntactic and semantic equality and can be weighted by parameter

## Repository Structure
* _CfgComparer_ - .NET console app for testing results 
* _CfgCompLib_  - .NET library with the graph comparison functionalities 
* _Testing_     - small collection of basic test cases (in xUnit)

## Tool configuration

The .NET library and .NET app are providing a JSON-Configuration file each:

* configuration of the .NET app:     _CfgComparerConfig.json_
```
{
  "Settings": {                     //example path:  "C:\\Folder\\"
    "cFileDir": "",                 //standard path for C files         -- default: inputs  folder in exec path
    "xmlFileDir": "",               //standard path for flow charts     -- default: inputs  folder in exec path
    "outFileDir": ""                //standard path for graph printout  -- default: outputs folder in exec path
  }
}
```

* configuration of the .NET library:  _CfgCompLibConfig.json_
```
{
  "Settings": {
    "CompilerOptimization": "-O1",                    //compiler optimization settings
    "CompilerDir": "",                                //provide path if GCC-compiler is not in windows env params
    "OutputDir": "",                                  //standard path for the compiled exec       -- default: outputs folder in exec path
    "XSDPath": ".\\validation\\FlowChart_Drawio.xsd"  //standard path for the XSD for validation  -- default: validation folder in lib
    //"XSDPath": ".\\validation\\FlowChart_Own.xsd"
  },

  "GraphEditCosts": {               //cost settings for edits in the graph edit distance calculation
    "NodeInsert":  2.0,                  
    "NodeDelete":  1.0,
    "NodeRelabel": 1.0,
    "EdgeInsert":  1.0,
    "EdgeDelete":  1.0
  },

  "LabelComparison": {              //weights for the metric which determines node label equality
    "QualtoQuantWeight":    0.5, 
    "LiteralWeight":        0.5, 
    "EqualityThreshold":    0.8  
  }
}
```

## Tool usage

* start the CfgComparer.exe with following parameters
    
          * <c-program>  "*.c"      - the C program to compare  
          * <flowchart>  "*.xml"    - the flow chart in XML
          * <optional>   "-p"       - for printing out both graphs in *.DOT-Format

* if help ist required:
  
                         "-help"    - shows the upper param info        

* after starting the tool the similarity of C program and flow chart is calculated and different additional info provided:

  * graph textual description (neighbourhood (predecessors and successors), Outdegree and Indegree)
    ![cmd_output1](https://github.com/user-attachments/assets/c1d20b32-2f31-4429-aa97-60d36f1ab0c0)

  * information of the maximum common connected subgraph & the node label comparison matchings
    ![cmd_output2](https://github.com/user-attachments/assets/4409ce16-2c08-40b1-a044-d8dad7ebcda7)

  * calculation of the graph edit distance & presentation of the evaluation results
    ![cmd_output3](https://github.com/user-attachments/assets/ce6e43c1-3b27-4eff-9aca-1002008ad44f)

## 3rd Party Licenses

Please refer to the [Link](https://github.com/Ini-D/CfgComparer/blob/main/THIRD-PARTY-NOTICES.TXT)
