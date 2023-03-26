using System;
using MessagePipe;

public class MyFilter<T> : MessageHandlerFilter<int>
{
    public override void Handle(int message, Action<int> next)
    {
        next(3);
    }
}