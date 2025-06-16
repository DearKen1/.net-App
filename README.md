This program is designed to track the condition of a man-made object. 
Since monitoring the man-made state of an object is not always possible for various reasons, or is simply difficult, simulation can come to the rescue.

For the program to work correctly, you need to create a database with the format .db or .sqlite, in which you must add two tables:
1. The first table with the name 'Данные', name the first column of this table as 'Эпоха' and number this column with numbers
ascending from 0 to, depending on how many epochs are present in your dimensions. Starting from the second column, the name
starts from 1 and so on in ascending order, depending on how many measurement points are in your object.
In addition to the cells in the first column, each cell must contain elevation values obtained using sensors located at control points (values in meters and separated by a dot).
2. The second table with the name 'Дополнительные_данные', it should contain 3 columns with 1 row in the first column you enter the value
measurement errors (the value is separated by a dot), the second is the number of blocks in your object (an integer), and the third
is the exponential smoothing value (a value greater than 0 and less than 1 is separated by a dot).
Exponential smoothing is a mathematical transformation method used in time series forecasting.
Measurement error is the deviation of the measured value of a quantity from its true (actual) value.
Also, to visually represent an object in the program, you can add images of the object by clicking on the corresponding icon with the name 'Add object diagram'.

At the first level of decomposition, we can use the calculated values "α" and "M" to plot the "Functions M(t)" as well as the graph "Phase coordinates α(M)".
From a geometric point of view:
α is the direction of movement of the point relative to the first state.
M is the length of the vector.

The second level of decomposition: We break down our system into blocks that we can build on the object diagram.
Now we can consider our blocks separately from each other to determine the most unstable, so that after that it would be possible to prevent emergencies.

The fourth level of decomposition
is the analysis of changes in the heights of the Z coordinates over time.
The user can select control points whose movements need to be studied.
