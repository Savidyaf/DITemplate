# DITemplate Work in progress
Unity Dependency Injection Architecture Template 

Made this boilerplate for my random hobby projects. I will update scripts here as it evolves. 

Goal is to create an easy to iterate framework for quick prototyping with future compatibility for production for any game I want to make. 
I write the code published to this repo first with each iteration and do a quick prototype with it to find the issues with my architecture.
My plan is to create a architecture that can deliver following goals with enough iterations. 

### Fast to implement 
- Should work for a  variety of games instead of one specific type or category. This is the reason I add features to this template with random prototypes I make. 
Simplifies the process of coming up with requirements

### Performance First 
- Infrastructure should establish high performance stanadards by default. 

### Ease of adding content
- Preferablly assets/data for the game needs to be added in a sustainable way for a small crew to manage.

I have been experimenting with how to optimize prototyping pipleline by creating generic service/data strcutures.


DI Framework : VContainer - https://vcontainer.hadashikick.jp/
Async wrapper : https://github.com/Cysharp/UniTask
Event Framework : https://github.com/Cysharp/MessagePipe


# Whats in progress 

## Demo Scene 
TODO : 
- Add a scene to demonstrate basic strcuture of the project

## Task System
1. Task mapping system to simplify content creation
2. Tasks should be able to program basic gameplay on a spreadsheet level
The main goal of this is to later integrate an in memeory managed database for relational access of data. 
TODO : 
- Add generic logic for task related content (Text,Prefabs) 
- Add sample world processor and a demo level 

## Data System
1. Maintain data in a scope level.
2. Local Storage logic
WIP : Added placeholder data logic.
TODO :
- Add MessagePack to data objects and compare serialisation results 
- Add MasterMemeory to handle relational data (This will be content specific)
  
## Event System 
WIP

## Content Management 
- Locally cached content/ Network delivered content 
WIP 

