using System.Collections.ObjectModel;
using System.Windows;
using TextBanner.Models;

namespace TextBanner.Services;

public class EventLogService
{
    private const int Max = 500;
    private readonly object _lock = new();

    public ObservableCollection<EventRecord> Records { get; } = new();

    public void Record(EventRecord record)
    {
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            lock (_lock)
            {
                Records.Insert(0, record);
                while (Records.Count > Max) Records.RemoveAt(Records.Count - 1);
            }
        });
    }

    public void Clear()
    {
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            lock (_lock) Records.Clear();
        });
    }
}
