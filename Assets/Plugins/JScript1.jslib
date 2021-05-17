mergeInto(
  LibraryManager.library,
  {
    FileUploaderInit: function() {
      var fileInput = document.getElementById('fileInput');
      if (!fileInput) {
        fileInput = document.createElement('input');
        fileInput.setAttribute('type', 'file');
        fileInput.setAttribute('id', 'fileInput');
      }
      fileInput.onclick = function (event) {
        this.value = null;
      };
      fileInput.onchange = function (event) {

        // document.getElementById('file').onchange = function(){

        //   var file = this.files[0];
        
        //   var reader = new FileReader();
        //   reader.onload = function(progressEvent){
        //     // Entire file
        //     console.log(this.result);
        
        //     // By lines
        //     var lines = this.result.split('\n');
        //     for(var line = 0; line < lines.length; line++){
        //       console.log(lines[line]);
        //     }
        //   };
        //   reader.readAsText(file);
        // };

        var files = event.target.files;
        for (var i = 0, f; f = files[i]; i++) {
          var objectPath = URL.createObjectURL(f);

          SendMessage('BrowserFileLoading', 'FileDialogResult', objectPath);
        }
      }
      document.body.appendChild(fileInput);
    }
  }
);