# SnelToetsenSjezer
Application to practise usage of Hot Keys  
  
### Current state:  
Partially refactored/rebuilt & not currently working, losing motivation from working at home because of the stupid public transport strike bs :(  
To finish the refactor i need too:  
- Check user-input-steps vs all possible solutions for a hotkey-question  
- Make use of the amount-of-questions input from main menu to get X amount of unique hotkey-questions in game, currently it just uses all hotkey-questions from selected categories  
- Make a proper callback for sending game-state to game screen/form  
- Need to rethink the interface of game, how to do the progressbar with the new logic of multiple solutions? (maybe start working with percentages? maybe just remove the progressbar?)  
  
### TODO:  
- [X] Implement a timer so we can figure out how long somebody took  
- [X] Turn 'Main menu' into some actual basic menu (show list of categories with checkboxes & input box for how many questions, with check on not being able to set amount of questions higher than max available questions)  
- [X] Come up with a way to have both 'AllAtOnce' and 'Sequential' in one hotkey, currently (with the provided examples) we do one or the other, but for example if you wanna have "make a comment in Visual Studio" as a question you cant set the solution to be Ctrl+K,Ctrl+C  
- [X] Come up with a way to have multiple solutions (for example, for the above both Ctrl+K,Ctrl+C -AND- Ctrl+K+C are valid & working solutions)  
- [X] Game logic should not be part of HotKeyService, split this up into KotKeyService & HotKeyGameService  
- [ ] ***[ !! Partially complete !! ]*** Refactor/cleanup code a bit  
- [ ] Implement a question correct/fail response in the game form that also pauses timer for a few seconds before continuing with game  
- [ ] Implement a gameover screen/form  
- [ ] Change how the repeating of questions works, while it currently works i didnt really follow the assignment close enough wich has lead to less than optimal user experience (once you start working on previously failed questions, if you fail one of those it just repeats right away, wich is odd)  
- [ ] Most of the logic is currently tied to keydown event, that works fine but doesnt leave any room for user-feedback, if they make a mistake or complete the question the application just instantly jumps to the next question, would be better if there was some sort of pause, feedback & then continue to next question. Also need this change to be able to even show the expected answer  

---

### Extended TODO:  
- [ ] A checkbox in main menu (default: checked) if you should be allowed to retry failed questions (maybe also a input box for how many retries, default: -1 for forever?) because it makes a lot more sense to me to 'accept' faulty awnsers (and then show those as failed in gameover screen) instead of giving the user unlimited retries for those  
- [ ] Try once more to add support for LCtrl/RCtrl/LShift/RShift etc (tried this early on and it was just rabbithole after rabbithole, appearantly this isnt a easy thing to achieve in C#..?! thats not acceptible to me though, i should just be able to read out actual keys instead of some interpertation)  
- [ ] Go over all special characters/keys of keyboard to make sure they are functional, its already being annoying with things like a backslash being called Oem5 or how Alt is sometimes detected as Alt, sometimes as AltKey, sometimes as Menu, etc. (seriously what even, shame on you C# for not being consistent wich such a basic thing)  
