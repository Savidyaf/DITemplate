# Unity Dependency Injected Data Architecture
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


# What is included

#Architecture 

- Common registration helper for Services to run all their initialization tasks in paralell
- MessagePipe integration for easy event registraion and handling. (WIP I'm still looking into better ways to implement async events)


## Data System
## Features
  ### Read Only Data - Once data is loaded, DI system will resolve the same instance accross the application.
  - Converts DB objects into a DataId : Data Blob dictionary that get's dequeued as data objects are used. I'm depending on the user class to determine if the data should return to the queue or not.
  ### Runtime Data Loading/Saving
  - Any runtime modified object will impement C#'s INotifyPropertyChanged. Usually used for UI bindings to mark the modified state of the object. There's Load / Save events that can be triggerd within scope or for the game lifetime scope to load/save data.
  - Data providers covert the type of the data object into a 64 digit string and use that as the mapping to load data blobs from the db.
  - There are additional benifits to this, DB can be encrypted if needed.


## In Progress 
- Examples for all data system use cases
- Event Driven UI Management System

### License
MIT
