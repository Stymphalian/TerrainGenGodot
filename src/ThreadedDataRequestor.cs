using Godot;
using System;
using System.Threading;
using System.Collections.Generic;


public partial class ThreadedDataRequestor : Node {
  class ThreadInfo {
    public readonly Action<object> Callback;
    public readonly object Data;

    public ThreadInfo(Action<object> callback, object data) {
      Callback = callback;
      Data = data;
    }
  };

  static ThreadedDataRequestor instance;
  private Queue<ThreadInfo> dataThreadQueue = new Queue<ThreadInfo>();

  public override void _Ready() {
    base._Ready();
    instance = this;
  }

  public override void _Process(double delta) {
    base._Process(delta);
    if (dataThreadQueue.Count > 0) {
      for (int index = 0; index < dataThreadQueue.Count; index++) {
        var threadData = dataThreadQueue.Dequeue();
        threadData.Callback(threadData.Data);
      }
    }
  }

  public static void RequestData(Func<object> generateData, Action<object> callback) {
    Thread dataThread = new Thread(() => {
      object data = generateData();
      lock (instance.dataThreadQueue) {
        instance.dataThreadQueue.Enqueue(new ThreadInfo(callback, data));
      }
    });
    dataThread.Start();
  }
}