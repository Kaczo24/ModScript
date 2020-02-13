let a = 0;
let c = 0;
let b = 0;

function add(x, y)
{
	return x + y;
}

function sw(x, y, z)
{
	if (y == "+")
		return add(a, z);
	if (y == "-")
		return x - z;
 	if (y == "*")
		return x * z;	
	return x / z;
}

while(true)
{
	a = InputN();
	c = Input();
	b = InputN();
	Printl(sw(a, c, b));
	Print("\n");
}