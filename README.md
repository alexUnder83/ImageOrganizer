ImageOrganizer is a programm to organize images using rules described a files structure. It watched the folder which path is extracted from the config file by "input" key. Image or video files that are added to this folder will be copied to the folder which path is defined in the config file with "output" key and ordered using rules.

Rules base on a file's date and define a folders structure khronologically. They extracted from the config file by "rules" key.
Date masks are used to filter files and put them to folders. Folder definitions are seperated by the slash symbol ("/") for nested folders and by the vertical bar ("|") for same level folders. Each definition contains a condition (is can use a mask) and a folder name (optionally) separated by colon (it also can use a mask). A static text is separated by the single quatation marks.
For example, consider a rule defines a folders structure "year/month":

yyyy' year'\
23.06.2013 - 05.07.2013:'June - July (Holydays)'|
MMMM\
\31.12.yyyy:'New Year'|
09.08.yyyy:'My Birthday'\
(mpg|mov):'Video'
