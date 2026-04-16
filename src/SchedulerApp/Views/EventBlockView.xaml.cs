using SchedulerApp.ViewModels;

namespace SchedulerApp.Views;

public partial class EventBlockView : ContentView
{
    public EventBlockView(CalendarEventViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
