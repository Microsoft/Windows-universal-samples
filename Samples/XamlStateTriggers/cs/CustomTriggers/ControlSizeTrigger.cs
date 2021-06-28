using Windows.UI.Xaml;

namespace StateTriggersSample.CustomTriggers
{
	public class ControlSizeTrigger : StateTriggerBase
	{
		//private variables
		private double _minControlHeight = -1, _minControlWidth = -1;
		private FrameworkElement _targetElement;
		private double _currentControlHeight, _currentControlWidth;
		//public properties to set from XAML
		public double MinControlHeight
		{
			get
			{
				return _minControlHeight;
			}
			set
			{
				_minControlHeight = value;
			}
		}
		public double MinControlWidth
		{
			get
			{
				return _minControlWidth;
			}
			set
			{
				_minControlWidth = value;
			}
		}
		public FrameworkElement TargetElement
		{
			get
			{
				return _targetElement;
			}
			set
			{
				if (_targetElement != null)
				{
					_targetElement.SizeChanged -= _targetElement_SizeChanged;
				}
				_targetElement = value;
				_targetElement.SizeChanged += _targetElement_SizeChanged;
			}
		}
		//Handle event to get current values
		private void _targetElement_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			_currentControlHeight = e.NewSize.Height;
			_currentControlWidth = e.NewSize.Width;
			UpdateTrigger();
		}
		//Logic to evaluate and apply trigger value
		private void UpdateTrigger()
		{
			//if target is set and either minHeight or minWidth is set, proceed
			if (_targetElement != null && (_minControlWidth >= 0 || _minControlHeight >= 0))
			{
				//if both minHeight and minWidth are set, then both conditions must be satisfied
				if (_minControlHeight >= 0 && _minControlWidth >= 0)
				{
					SetActive((_currentControlHeight >= _minControlHeight) && (_currentControlWidth >= _minControlWidth));
				}
				//if only one of them is set, then only that condition needs to be satisfied
				else if (_minControlHeight >= 0)
				{
					SetActive(_currentControlHeight >= _minControlHeight);
				}
				else
				{
					SetActive(_currentControlWidth >= _minControlWidth);
				}
			}
			else
			{
				SetActive(false);
			}
		}
	}
}
