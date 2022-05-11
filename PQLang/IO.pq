fun ReadFromFile(path) {
	if (path.type != "String") { error("Path must be a string!") }
	interopFunction("fileReadLines", path)
}

//text can be both string and array of strings
fun WriteToFile(path, text) {
	if (path.type != "String") { error("Path must be a string!") }
	if (text.type == "Array") {
		interopFunction("fileWriteLines", path, text)
	}
	else {
		if (text.type == "String") {
			interopFunction("fileWriteText", path, text)
		}
		else {
			error("Text must either be a string or an array of strings")
		}
	}
}

//text can be both string and array of strings
fun AppendToFile(path, text){
	if (path.type != "String") { error("Path must be a string!") }
	if (text.type == "Array") {
		interopFunction("fileAppendLines", path, text)
	}
	else{
		if (text.type == "String") {
			interopFunction("fileAppendText", path, text)
		}
		else {
			error("Text must either be a string or an array of strings")
		}
	}
}

fun CreateFile(path) {
	if (path.type != "String") { error("Path must be a string!") }
	interopFunction("createFile", path)
}

fun DeleteFile(path) {
	if (path.type != "String") { error("Path must be a string!") }
	interopFunction("deleteFile", path)
}

fun CreateFolder(path) {
	if (path.type != "String") { error("Path must be a string!") }
	interopFunction("createFolder", path)
}

fun DeleteFolder(path) {
	if (path.type != "String") { error("Path must be a string!") }
	interopFunction("deleteFolder", path)
}

fun GetFileNames(path) {
	if (path.type != "String") { error("Path must be a string!") }
	interopFunction("getFileNames", path)
}

fun GetFolderNames(path) {
	if (path.type != "String") { error("Path must be a string!") }
	interopFunction("getFolderNames", path)
}