mergeInto(
  LibraryManager.library,
  {
    FocusFileUploader: function () {
      var fileuploader = document.getElementById('fileuploader');
      if (fileuploader) {
          fileuploader.setAttribute('class', 'focused');
      }
    }
  }
);