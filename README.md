# exui-wpf
frontend receiver of exui-api

**under progress. not ready for casual usage. Proof of Concept**

## What
**Goal**: to create new ui elements to nfsu2 with ease.

**Method**: transparent native os windows to mimic in game ui

**Advantages**: very easy to create, customize and switch templates. takes 5-10 minutes to create a new gui element depending on the complexity and wpf skills.

**Backend**: receive telemetry data from a websocket via [exui-api](https://github.com/clod44/exui-api)

![screenshot.png](screenshot.png)

## Project
you are expected to have this kind of folder structure
```
exui/
├── exui-api/
│   └── exui-api.dll
├── exui-wpf/
│   └── exui-wpf.dll
└── templates/
    └── Speedometer/ (a template)
        └── Speedometer.dll
```
## Templates
- the `templates` folder contains the ui elements you can use.  
- the `exui-wpf.exe` is used for template selection and telemetry accession.
- you can inspect and create a clone of the [Speedometer](https://github.com/clod44/Speedometer) template, change the names and change the `.xaml` file to create a new template. you have all the reach of a `.wpf` file and a constructor `.cs` file which you can expand from. 
- for practice, keep your template content contained within your template folder.
## Telemetry data
### .wpf
```xml 
Value={Binding Telemetry[gear]}
```
the "gear" keyword here is in the incoming websocket data key. see backend configuration: [exui-api variables.txt](https://github.com/clod44/exui-api/blob/main/variables.txt)


get all data: 
```xml
<ItemsControl ItemsSource="{Binding TelemetryRows}">
```

### .cs
basically
```cs
private MainWindow? _hostContext;

...
{
    double speed = Convert.ToDouble(_hostContext.Telemetry["speed"]);
                
}
```
<hr>

**fork/commit encouraged**