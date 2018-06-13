ImageOrganizer is a programm to organize images using rules described a files structure.

Rules base on a file's date and define a folders structure khronologically.
Date masks are used to filter files and put them to folders. Folder definitions are seperated by the slash symbol ("/") for nested folders and by the vertical bar ("|") for same level folders. Each definition contains a condition (is can use a mask) and a folder name (optionally) separated by colon (it also can use a mask).
For example, consider a rule defines a folders structure "year/month":

yyyy' year'\
23.06.2013 - 05.07.2013:'June - July (Holydays)'|
MMMM\
\31.12.yyyy:'New Year'|
09.08.yyyy:'My Birthday'\
(mpg|mov):'Video'

A static text is separated by the single quatation marks.