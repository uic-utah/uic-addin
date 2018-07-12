using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;

namespace uic_addin.ViewModels {
    public class PropertyChangedWithValidation : INotifyPropertyChanged, INotifyDataErrorInfo {
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged = delegate { };
        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        private readonly Dictionary<string, List<string>> _errors = new Dictionary<string, List<string>>();
        private readonly ObservableCollection<string> _observableErrors = new ObservableCollection<string>();

        public ReadOnlyObservableCollection<string> ErrorValues => new ReadOnlyObservableCollection<string>(_observableErrors);

        protected void SetProperty<T>(ref T member, T val, [CallerMemberName] string propertyName = null) {
            if (Equals(member, val)) {
                return;
            }

            ValidateProperty(propertyName, val);

            member = val;
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ValidateProperty<T>(string propertyName, T value) {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(this) {
                MemberName = propertyName
            };
            Validator.TryValidateProperty(value, context, results);

            if (results.Any()) {
                _errors[propertyName] = results.Select(c => c.ErrorMessage).ToList();
                _errors[propertyName].ForEach(x => {
                    if (_observableErrors.Contains(x)) {
                        return;
                    }

                    _observableErrors.Add(x);
                });
            } else {
                if (_errors.ContainsKey(propertyName)) {
                    var values = _errors[propertyName];
                    values.ForEach(x => _observableErrors.Remove(x));
                }

                _errors.Remove(propertyName);
            }

            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        public IEnumerable GetErrors(string propertyName) {
            if (_errors.ContainsKey(propertyName)) {
                return _errors[propertyName];
            }

            return null;
        }

        public bool HasErrors => _errors.Count > 0;
    }
}
