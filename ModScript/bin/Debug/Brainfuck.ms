let tape = [0, 0, 0];
let instruction = "";
let index = 0;
let hedI = 0;
let s = "";
let c = 0;


instruction = Input();

while(index < instruction.length)
{
	s = instruction[index];
	if(s == "<")
		hedI -= 1;
	if(s == ">")
		hedI += 1;
	if(s == "+")
		tape[hedI] += 1;
	if(s == "-")
		tape[hedI] -= 1;
	if(s == ".")
		PrintAscii(tape[hedI]);
	if(s == ",")
		tape[hedI] = InputN();
	if(s == "[")
		if(tape[hedI] == 0)
		{
			c = 1;
			while(c > 0)
			{
				index += 1;
				if(instruction[index] == "[")
					c += 1;
				if(instruction[index] == "]")
					c -= 1;
			}
		}
	if(s == "]")
		if(tape[hedI] != 0)
		{
			c = 1;
			while(c > 0)
			{
				index -= 1;
				if(instruction[index] == "]")
					c += 1;
				if(instruction[index] == "[")
					c -= 1;
			}
		}
	
	
	if(hedI >= tape.length)
		tape += [0];
	if(hedI < 0)
	{
		tape = [0] + tape;
		hedI += 1;
	}
	while(tape[hedI] < 0)
		tape[hedI] += 256;
	while(tape[hedI] > 255)
		tape[hedI] -= 256;
	index += 1;
}