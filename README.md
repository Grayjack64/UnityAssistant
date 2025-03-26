# Unity AI Coding Assistant

A Unity Editor plugin that integrates AI models (Grok, Claude, and local LLMs via Ollama) directly into the Unity Editor to assist with coding tasks.

## Features

- **AI Integration**: Connect to Grok, Claude, or local LLMs (via Ollama)
- **Console Analysis**: Have the AI read and analyze Unity console output
- **Code Review**: Review project code directly within the Unity Editor
- **Code Modification**: Apply AI-suggested changes to your code
- **Code Generation**: Create new scripts based on natural language requirements

## Requirements

- Unity 2019.4 or later
- .NET 4.x Scripting Runtime
- API keys for Grok and/or Claude (if using those services)
- Ollama installed locally (if using local LLMs)

## Installation

1. Download the latest release or clone this repository
2. Import the package into your Unity project
3. Access the AI Coding Assistant from the Window menu in Unity

## Setup

### API Keys
To use Grok or Claude, you'll need to set up API keys:
1. Open the AI Coding Assistant window (Window > AI Coding Assistant)
2. Go to Settings
3. Enter your API keys

### Local LLMs (Ollama)
To use local LLMs:
1. Install Ollama from [ollama.ai](https://ollama.ai)
2. Run Ollama locally (default: http://localhost:11434)
3. Select "Local LLM" in the AI Coding Assistant

## Usage

### Analyzing Code
1. Select a script in your project
2. Open the AI Coding Assistant
3. Choose "Analyze Code"
4. Review and apply suggested changes

### Generating Code
1. Open the AI Coding Assistant
2. Choose "Generate Code"
3. Describe what you want the script to do
4. Specify a name for the new script
5. Click "Generate" and review the results

## Development

This project is structured as a Unity Editor plugin. All code is contained within the Assets/Editor folder to ensure it only runs in the Unity Editor.

### Project Structure
- `Assets/Editor/AICodingAssistant/`: Main plugin folder
  - `Editor/`: Editor scripts
  - `Scripts/`: Plugin core functionality
  - `UI/`: UI-related components
  - `AI/`: AI backend integrations

## License

[MIT License](LICENSE) 