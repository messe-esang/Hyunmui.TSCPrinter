# tsclibnet.Core

이 라이브러리는 프린터 제조사인 TSC 에서 제공되는 `Windows Framework DLL` 를 디컴파일해서 `.Net Standard`로 컨버팅시킨 라이브러리입니다. 개인용으로 쓰려고 버그가 있던 일부분을 제외하고는 건드리지 않았습니다.

수정 부분은 다음과 같습니다

- WinForms 종속성 제거 및 TscException 예외 처리로 변경
- `usb.printsetting()`이 정상 작동하지 않는 버그 수정
