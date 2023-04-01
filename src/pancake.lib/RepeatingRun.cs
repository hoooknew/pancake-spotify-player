using pancake.lib;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class RepeatingRun
{
    private readonly Func<CancellationToken, Task> _task;
    private readonly int _delayMS;
    private CancellationTokenSource _cancel;
    private Task? _repeatingTask;

    public RepeatingRun(Func<CancellationToken, Task> task, int delayBetweenRunsMS)
    {
        _task = task;
        _delayMS = delayBetweenRunsMS;
        _cancel = new CancellationTokenSource();
    }

    public void Start()
    {
        Stop();

        _repeatingTask = Task.Run(_repeater, _cancel.Token);
    }
    public void Stop()
    {
        if (this._cancel != null)
        {
            _cancel.Cancel();
            _cancel.Token.WaitHandle.WaitOne(1000);
            _cancel.Dispose();
            _cancel = new CancellationTokenSource();
        }

        if (_repeatingTask != null)
        {
            if (_repeatingTask.Status != TaskStatus.WaitingForActivation)
                _repeatingTask.Dispose();
            _repeatingTask = null;
        }
    }

    public bool IsRunning => _repeatingTask != null;

    public async Task Invoke()
    {
        bool isRunning = IsRunning;

        Stop();

        await _task(_cancel.Token);

        if (isRunning)
            Start();
    }

    private async Task _repeater()
    {
        var cancelToken = this._cancel!.Token;
        var timer = new PeriodicTimer(_delayMS.MSasTimeSpan());

        while (!_cancel.IsCancellationRequested)
        {
            await timer.WaitForNextTickAsync(cancelToken);

            await _task(_cancel.Token);
        }
    }
}
