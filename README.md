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


# What is Working

#Architecture 

- Common registration helper for Services to run all their initialization tasks in paralell
- MessagePipe integration for easy event registraion and handling. (WIP I'm still looking into better ways to implement async events)


## Data System
- Data system phase 1 was to use a SQL db to store serialized data. I'm not taking full advantage of having a DB in this version but good example for what I want to use it for. 

### License
MIT
