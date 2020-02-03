let s = ReadText("name.txt");
let l = 0;
let b = true;
while(b)
{
	l = Input();
	if(l == "EXIT")
		b = false;
	else
		s += "\n" + l;
}
WriteText("name.txt", s);
