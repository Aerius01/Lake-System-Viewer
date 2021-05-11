mergeInto(
  LibraryManager.library,
  {
    FileUploaderInit: function() {
      var fileInput = document.getElementById('fileInput');
      if (!fileInput) {
        fileInput = document.createElement('fileInput');
        fileInput.setAttribute('type', 'file');
        fileInput.setAttribute('id', 'fileuploader');
      }
      fileInput.onclick = function (event) {
        this.value = null;
      };
      fileInput.onchange = function (event) {
        var files = e.target.files;
        for (var i = 0, f; f = files[i]; i++) {
          window.alert(URL.createObjectURL(f));
          SendMessage('BrowserFileLoading', 'FileDialogResult', URL.createObjectURL(f));
        }
      }
      document.body.appendChild(fileInput);
    }
  }
);