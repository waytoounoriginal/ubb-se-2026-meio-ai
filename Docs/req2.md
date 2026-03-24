Assignment 2
Now that the client trusts your team with the direction, it’s time to show off our coding skills.
•
Implement your design as a WinUI 3 app that uses MVVM UI pattern. Yes, the entire app.
•
Verification/grading of your implementation will be based on the requirements and design that you handed in for assignment 1.
•
Upload your project to your GitHub repository created at assignment 1. If prompted, use merge (not rebase) for git. (I very strongly recommend creating separate branches for each task, when that task is done merge the main branch on the task branch, make sure everything is working then create a pull request to merge back into the main branch.)
•
Your code and class diagram must be 1-to-1, if you deviated from the initial plan then update either the code or the diagram. A generated class diagram is worth 0 points. When implementing your domain use classes to model relationships and not foreign keys.
•
Your application should store its data long term; this must be done using a local installation of SQL server. The SQL queries must be written by hand, no ORMs are accepted. You may not use stored procedures. Business logic must be absent from your queries. The SQL server may not be online (for example may not be azure) and may not be any other type of SQL client.
•
If you need the structure of entities or function calls developed by some other team, you can look at their documentation at the git repository they’ve shared with your team lead. If you need some information that they would provide (e.g. user data), then just copy the info from there since they have committed to implementing it that way. You can create mock-up version of their service(es) using only the functions that you need that return hard coded entities of that type (e.g. UserService.GetCurrentUser()).
•
Unit or integration tests are not required.
•
For now, there is no need for a server. Interactions should go through the data storage if you need one. Let’s say you need to send a file to another user; you would save that file locally and store the location. If you logged in as that other user, you would display that they got a file from someone, and you would fetch it from the local disk. Anything direct like a voice call you can do either by sending files this way or connecting through static IP-s on the local network.
•
ATTENTION: You are not familiar with working in teams and your current strategies of doing assignments do not work in a team. Assume that everything takes a lot longer than what your instincts tell you. Try to aim to be done 2-3 days before you should be, at least until you figure out you team’s dynamics. If you fall
behind, then you will have less time for the next one, and it compounds. Experience shows that it’s really hard to get back on track.
Ongoing requirements:
•
The team lead will present a report before demoing their work on the contributions of each team member, ideally task/git/chat/sent files history etc. With the scope of proving that each team member did some non-negligible amount of work.
•
Double check that you shared your work with everybody that you should have.
Optional: Use tasks for work tracking and management.
Deadline: lab 3